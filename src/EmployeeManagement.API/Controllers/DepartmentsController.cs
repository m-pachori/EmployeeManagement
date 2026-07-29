using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.DepartmentsRead)]
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
    {
        var departments = await _unitOfWork.Repository<Department>().Query()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.Description,
                x.IsActive,
                employeeCount = x.Employees.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(departments);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.DepartmentsRead)]
    public async Task<IActionResult> GetDepartmentById(int id, CancellationToken cancellationToken)
    {
        var department = await _unitOfWork.Repository<Department>().Query()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.Description,
                x.IsActive,
                employeeCount = x.Employees.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (department is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Department not found.");
        }

        return Ok(department);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.DepartmentsWrite)]
    public async Task<IActionResult> CreateDepartment([FromBody] UpsertDepartmentRequest request, CancellationToken cancellationToken)
    {
        await ValidateDepartmentAsync(request, cancellationToken);

        var department = new Department
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name
        };

        await _unitOfWork.Repository<Department>().AddAsync(department, cancellationToken);
        await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserId = GetCurrentUserId(),
            EventType = "DepartmentCreate",
            EntityName = nameof(Department),
            Details = $"Created department '{department.Name}'.",
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetDepartmentById), new { id = department.Id, version = "1" }, department.Id);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.DepartmentsWrite)]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpsertDepartmentRequest request, CancellationToken cancellationToken)
    {
        var department = await _unitOfWork.Repository<Department>().Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Department not found.");

        await ValidateDepartmentAsync(request, cancellationToken, id);

        department.Name = request.Name.Trim();
        department.Code = request.Code.Trim();
        department.Description = request.Description?.Trim();
        department.IsActive = request.IsActive;
        department.UpdatedBy = User.Identity?.Name;
        department.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserId = GetCurrentUserId(),
            EventType = "DepartmentUpdate",
            EntityName = nameof(Department),
            EntityId = department.Id.ToString(),
            Details = $"Updated department '{department.Name}'.",
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Department updated successfully." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.DepartmentsWrite)]
    public async Task<IActionResult> DeleteDepartment(int id, CancellationToken cancellationToken)
    {
        var department = await _unitOfWork.Repository<Department>().Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Department not found.");

        var hasEmployees = await _unitOfWork.Repository<Employee>().Query().AnyAsync(x => x.DepartmentId == id, cancellationToken);
        if (hasEmployees)
        {
            throw new ApiException(StatusCodes.Status409Conflict,
                "Department cannot be deleted because employees are assigned to it.");
        }

        _unitOfWork.Repository<Department>().Remove(department);
        await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserId = GetCurrentUserId(),
            EventType = "DepartmentDelete",
            EntityName = nameof(Department),
            EntityId = department.Id.ToString(),
            Details = $"Deleted department '{department.Name}'.",
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Department deleted successfully." });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out var userId) ? userId : null;
    }

    private async Task ValidateDepartmentAsync(UpsertDepartmentRequest request, CancellationToken cancellationToken, int? existingDepartmentId = null)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Department name and code are required.");
        }

        var duplicateName = await _unitOfWork.Repository<Department>().Query()
            .AnyAsync(x => x.Name == request.Name && x.Id != existingDepartmentId, cancellationToken);
        if (duplicateName)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Department name already exists.");
        }

        var duplicateCode = await _unitOfWork.Repository<Department>().Query()
            .AnyAsync(x => x.Code == request.Code && x.Id != existingDepartmentId, cancellationToken);
        if (duplicateCode)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Department code already exists.");
        }
    }
}

public class UpsertDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}