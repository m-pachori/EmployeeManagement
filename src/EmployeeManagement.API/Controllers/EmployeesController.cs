using System.Text;
using System.Text.RegularExpressions;
using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees")]
[Authorize]
public class EmployeesController : ApiControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;
    private readonly IAuditLogService _auditLogService;

    public EmployeesController(IUnitOfWork unitOfWork, IWebHostEnvironment environment, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _environment = environment;
        _auditLogService = auditLogService;
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
        (page, pageSize) = ClampPagination(page, pageSize);

        var query = _unitOfWork.Repository<Employee>().Query()
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Manager)
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
                x.Designation,
                x.Salary,
                x.DateOfJoining,
                Status = x.Status.ToString(),
                Department = x.Department.Name,
                x.DepartmentId,
                x.ManagerId,
                ManagerName = x.Manager == null ? null : x.Manager.FirstName + " " + x.Manager.LastName,
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
        var employee = await _unitOfWork.Repository<Employee>().Query()
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Manager)
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
                x.Designation,
                x.Salary,
                x.DateOfJoining,
                Status = x.Status.ToString(),
                x.DepartmentId,
                Department = x.Department.Name,
                x.ManagerId,
                ManagerName = x.Manager == null ? null : x.Manager.FirstName + " " + x.Manager.LastName,
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
            Designation = request.Designation?.Trim(),
            Salary = request.Salary,
            DateOfJoining = request.DateOfJoining,
            Status = request.Status,
            DepartmentId = request.DepartmentId,
            ManagerId = request.ManagerId,
            CreatedBy = User.Identity?.Name ?? "system",
            UpdatedBy = User.Identity?.Name ?? "system"
        };

        await _unitOfWork.Repository<Employee>().AddAsync(employee, cancellationToken);
        await RecordAuditLogAsync(_auditLogService, "EmployeeCreate", nameof(Employee), null,
            $"Created employee '{employee.EmployeeCode}'.", cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetEmployeeById), new { id = employee.Id, version = "1" }, employee.Id);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.EmployeesWrite)]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var employee = await _unitOfWork.Repository<Employee>().Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Employee not found.");

        await ValidateEmployeeRequestAsync(new CreateEmployeeRequest
        {
            EmployeeCode = request.EmployeeCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Designation = request.Designation,
            Salary = request.Salary,
            DateOfJoining = request.DateOfJoining,
            Status = request.Status,
            DepartmentId = request.DepartmentId,
            ManagerId = request.ManagerId
        }, cancellationToken, id);

        employee.EmployeeCode = request.EmployeeCode.Trim();
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Email = request.Email.Trim();
        employee.PhoneNumber = request.PhoneNumber?.Trim();
        employee.Designation = request.Designation?.Trim();
        employee.Salary = request.Salary;
        employee.DateOfJoining = request.DateOfJoining;
        employee.Status = request.Status;
        employee.DepartmentId = request.DepartmentId;
        employee.ManagerId = request.ManagerId;
        employee.UpdatedBy = User.Identity?.Name ?? "system";
        employee.UpdatedDate = DateTime.UtcNow;

        await RecordAuditLogAsync(_auditLogService, "EmployeeUpdate", nameof(Employee), employee.Id.ToString(),
            $"Updated employee '{employee.EmployeeCode}'.", cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Employee updated successfully." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.EmployeesWrite)]
    public async Task<IActionResult> DeleteEmployee(int id, CancellationToken cancellationToken)
    {
        var employee = await _unitOfWork.Repository<Employee>().Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Employee not found.");

        _unitOfWork.Repository<Employee>().Remove(employee);
        await RecordAuditLogAsync(_auditLogService, "EmployeeDelete", nameof(Employee), employee.Id.ToString(),
            $"Deleted employee '{employee.EmployeeCode}'.", cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Employee deleted successfully." });
    }

    private const long MaxPhotoSizeInBytes = 250 * 1024;
    private static readonly string[] AllowedPhotoExtensions = [".jpg", ".jpeg"];
    private static readonly string[] AllowedPhotoContentTypes = ["image/jpeg"];

    [HttpPost("{id:int}/photo")]
    [Authorize(Policy = Permissions.EmployeesWrite)]
    [RequestSizeLimit(MaxPhotoSizeInBytes)]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Photo file is required.");
        }

        if (file.Length > MaxPhotoSizeInBytes)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Photo file size must not exceed 250 KB.");
        }

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
        if (!AllowedPhotoExtensions.Contains(extension) || !AllowedPhotoContentTypes.Contains(contentType))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Only JPG photo files are allowed.");
        }

        var employee = await _unitOfWork.Repository<Employee>().Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Employee not found.");

        var uploadsRoot = Path.Combine(_environment.ContentRootPath, "uploads", "employees", id.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"photo_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        var absolutePath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        employee.PhotoUrl = $"/uploads/employees/{id}/{fileName}";
        employee.UpdatedBy = User.Identity?.Name ?? "system";
        employee.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Photo uploaded successfully.", employee.PhotoUrl });
    }

    [HttpGet("export/csv")]
    [Authorize(Policy = Permissions.ReportsRead)]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken)
    {
        var rows = await _unitOfWork.Repository<Employee>().Query()
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

    private static readonly Regex PhoneRegex = new(@"^\+?[0-9\s\-()]{7,20}$", RegexOptions.Compiled);

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

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && !PhoneRegex.IsMatch(request.PhoneNumber))
        {
            throw new ApiException(StatusCodes.Status400BadRequest,
                "Phone number must contain only digits, spaces, and the characters + - ( ) and be 7-20 characters long.");
        }

        if (request.DateOfJoining.Date > DateTime.UtcNow.Date)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Date of joining cannot be in the future.");
        }

        if (request.Salary.HasValue && request.Salary.Value <= 0)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Salary must be a positive value.");
        }

        var departmentExists = await _unitOfWork.Repository<Department>().Query()
            .AnyAsync(x => x.Id == request.DepartmentId && x.IsActive, cancellationToken);
        if (!departmentExists)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Department does not exist or is inactive.");
        }

        if (request.ManagerId.HasValue)
        {
            if (request.ManagerId.Value == existingEmployeeId)
            {
                throw new ApiException(StatusCodes.Status400BadRequest, "An employee cannot be their own manager.");
            }

            var managerExists = await _unitOfWork.Repository<Employee>().Query()
                .AnyAsync(x => x.Id == request.ManagerId.Value, cancellationToken);
            if (!managerExists)
            {
                throw new ApiException(StatusCodes.Status400BadRequest, "Manager does not exist.");
            }
        }

        var duplicateCode = await _unitOfWork.Repository<Employee>().Query()
            .AnyAsync(x => x.EmployeeCode == request.EmployeeCode && x.Id != existingEmployeeId, cancellationToken);
        if (duplicateCode)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Employee code already exists.");
        }

        var duplicateEmail = await _unitOfWork.Repository<Employee>().Query()
            .AnyAsync(x => x.Email == request.Email && x.Id != existingEmployeeId, cancellationToken);
        if (duplicateEmail)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Employee email already exists.");
        }
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
    public string? Designation { get; set; }
    public decimal? Salary { get; set; }
    public DateTime DateOfJoining { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public int DepartmentId { get; set; }
    public int? ManagerId { get; set; }
}

public class UpdateEmployeeRequest
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Designation { get; set; }
    public decimal? Salary { get; set; }
    public DateTime DateOfJoining { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public int DepartmentId { get; set; }
    public int? ManagerId { get; set; }
}