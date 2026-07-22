using System.Text;
using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;
using EmployeeManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public EmployeesController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.EmployeesRead)]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] string? search,
        [FromQuery] int? departmentId,
        [FromQuery] EmployeeStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "createdDate",
        [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Employees
            .AsNoTracking()
            .Include(x => x.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(x =>
                x.FirstName.ToLower().Contains(keyword)
                || x.LastName.ToLower().Contains(keyword)
                || x.Email.ToLower().Contains(keyword)
                || x.EmployeeCode.ToLower().Contains(keyword));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(x => x.DepartmentId == departmentId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        query = (sortBy.ToLowerInvariant(), sortDirection.ToLowerInvariant()) switch
        {
            ("firstname", "asc") => query.OrderBy(x => x.FirstName),
            ("firstname", _) => query.OrderByDescending(x => x.FirstName),
            ("lastname", "asc") => query.OrderBy(x => x.LastName),
            ("lastname", _) => query.OrderByDescending(x => x.LastName),
            ("email", "asc") => query.OrderBy(x => x.Email),
            ("email", _) => query.OrderByDescending(x => x.Email),
            ("dateofjoining", "asc") => query.OrderBy(x => x.DateOfJoining),
            ("dateofjoining", _) => query.OrderByDescending(x => x.DateOfJoining),
            ("createddate", "asc") => query.OrderBy(x => x.CreatedDate),
            _ => query.OrderByDescending(x => x.CreatedDate)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.EmployeeCode,
                x.FirstName,
                x.LastName,
                x.Email,
                x.PhoneNumber,
                x.PhotoUrl,
                x.DateOfJoining,
                Status = x.Status.ToString(),
                Department = x.Department.Name,
                x.DepartmentId,
                x.CreatedDate
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            items = data
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.EmployeesRead)]
    public async Task<IActionResult> GetEmployeeById(int id, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Documents)
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.EmployeeCode,
                x.FirstName,
                x.LastName,
                x.Email,
                x.PhoneNumber,
                x.PhotoUrl,
                x.DateOfJoining,
                Status = x.Status.ToString(),
                x.DepartmentId,
                Department = x.Department.Name,
                documents = x.Documents.Select(d => new
                {
                    d.Id,
                    d.FileName,
                    d.FilePath,
                    d.ContentType,
                    d.SizeInBytes,
                    d.UploadedAtUtc
                })
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Employee not found.");
        }

        return Ok(employee);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EmployeesWrite)]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        await ValidateEmployeeRequestAsync(request, cancellationToken);

        var employee = new Employee
        {
            EmployeeCode = request.EmployeeCode.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            DateOfJoining = request.DateOfJoining,
            Status = request.Status,
            DepartmentId = request.DepartmentId,
            CreatedBy = User.Identity?.Name ?? "system",
            UpdatedBy = User.Identity?.Name ?? "system"
        };

        _context.Employees.Add(employee);
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = GetCurrentUserId(),
            EventType = "EmployeeCreate",
            EntityName = nameof(Employee),
            Details = $"Created employee '{employee.EmployeeCode}'.",
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetEmployeeById), new { id = employee.Id, version = "1" }, employee.Id);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.EmployeesWrite)]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Employee not found.");

        await ValidateEmployeeRequestAsync(new CreateEmployeeRequest
        {
            EmployeeCode = request.EmployeeCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            DateOfJoining = request.DateOfJoining,
            Status = request.Status,
            DepartmentId = request.DepartmentId
        }, cancellationToken, id);

        employee.EmployeeCode = request.EmployeeCode.Trim();
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Email = request.Email.Trim();
        employee.PhoneNumber = request.PhoneNumber?.Trim();
        employee.DateOfJoining = request.DateOfJoining;
        employee.Status = request.Status;
        employee.DepartmentId = request.DepartmentId;
        employee.UpdatedBy = User.Identity?.Name ?? "system";
        employee.UpdatedDate = DateTime.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = GetCurrentUserId(),
            EventType = "EmployeeUpdate",
            EntityName = nameof(Employee),
            EntityId = employee.Id.ToString(),
            Details = $"Updated employee '{employee.EmployeeCode}'.",
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Employee updated successfully." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.EmployeesWrite)]
    public async Task<IActionResult> DeleteEmployee(int id, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Employee not found.");

        _context.Employees.Remove(employee);
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = GetCurrentUserId(),
            EventType = "EmployeeDelete",
            EntityName = nameof(Employee),
            EntityId = employee.Id.ToString(),
            Details = $"Deleted employee '{employee.EmployeeCode}'.",
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Employee deleted successfully." });
    }

    [HttpPost("{id:int}/photo")]
    [Authorize(Policy = Permissions.EmployeesWrite)]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Photo file is required.");
        }

        var employee = await _context.Employees.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Employee not found.");

        var uploadsRoot = Path.Combine(_environment.ContentRootPath, "uploads", "employees", id.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"photo_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        var absolutePath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        employee.PhotoUrl = $"/uploads/employees/{id}/{fileName}";
        employee.UpdatedBy = User.Identity?.Name ?? "system";
        employee.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Photo uploaded successfully.", employee.PhotoUrl });
    }

    [HttpGet("export/csv")]
    [Authorize(Policy = Permissions.ReportsRead)]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken)
    {
        var rows = await _context.Employees
            .AsNoTracking()
            .Include(x => x.Department)
            .OrderBy(x => x.EmployeeCode)
            .Select(x => new
            {
                x.EmployeeCode,
                x.FirstName,
                x.LastName,
                x.Email,
                x.PhoneNumber,
                Department = x.Department.Name,
                Status = x.Status.ToString(),
                x.DateOfJoining
            })
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("EmployeeCode,FirstName,LastName,Email,PhoneNumber,Department,Status,DateOfJoining");

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',',
                EscapeCsv(row.EmployeeCode),
                EscapeCsv(row.FirstName),
                EscapeCsv(row.LastName),
                EscapeCsv(row.Email),
                EscapeCsv(row.PhoneNumber),
                EscapeCsv(row.Department),
                EscapeCsv(row.Status),
                row.DateOfJoining.ToString("yyyy-MM-dd")));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"employees_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private async Task ValidateEmployeeRequestAsync(CreateEmployeeRequest request, CancellationToken cancellationToken, int? existingEmployeeId = null)
    {
        if (string.IsNullOrWhiteSpace(request.EmployeeCode)
            || string.IsNullOrWhiteSpace(request.FirstName)
            || string.IsNullOrWhiteSpace(request.LastName)
            || string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ApiException(StatusCodes.Status400BadRequest,
                "Employee code, first name, last name, and email are required.");
        }

        var departmentExists = await _context.Departments
            .AnyAsync(x => x.Id == request.DepartmentId && x.IsActive, cancellationToken);
        if (!departmentExists)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Department does not exist or is inactive.");
        }

        var duplicateCode = await _context.Employees
            .AnyAsync(x => x.EmployeeCode == request.EmployeeCode && x.Id != existingEmployeeId, cancellationToken);
        if (duplicateCode)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Employee code already exists.");
        }

        var duplicateEmail = await _context.Employees
            .AnyAsync(x => x.Email == request.Email && x.Id != existingEmployeeId, cancellationToken);
        if (duplicateEmail)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Employee email already exists.");
        }
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out var userId) ? userId : null;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

public class CreateEmployeeRequest
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime DateOfJoining { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public int DepartmentId { get; set; }
}

public class UpdateEmployeeRequest
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime DateOfJoining { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public int DepartmentId { get; set; }
}