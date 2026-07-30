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
[Route("api/v{version:apiVersion}/roles")]
[Authorize]
public class RolesController : ApiControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public RolesController(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.RolesRead)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _unitOfWork.Repository<Role>().Query()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                permissionCount = x.RolePermissions.Count,
                userCount = x.UserRoles.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(roles);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.RolesWrite)]
    public async Task<IActionResult> CreateRole([FromBody] UpsertRoleRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Role name is required.");
        }

        var exists = await _unitOfWork.Repository<Role>().Query().AnyAsync(x => x.Name == request.Name, cancellationToken);
        if (exists)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Role already exists.");
        }

        var role = new Role
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name
        };

        await _unitOfWork.Repository<Role>().AddAsync(role, cancellationToken);
        await RecordAuditLogAsync(_auditLogService, "RoleCreate", nameof(Role), null,
            $"Created role '{role.Name}'.", cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetRoles), new { version = "1" }, role.Id);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.RolesWrite)]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpsertRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await _unitOfWork.Repository<Role>().Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Role not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Role name is required.");
        }

        var duplicate = await _unitOfWork.Repository<Role>().Query().AnyAsync(x => x.Name == request.Name && x.Id != id, cancellationToken);
        if (duplicate)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Role already exists.");
        }

        role.Name = request.Name.Trim();
        role.Description = request.Description?.Trim() ?? string.Empty;
        role.UpdatedBy = User.Identity?.Name;
        role.UpdatedDate = DateTime.UtcNow;

        await RecordAuditLogAsync(_auditLogService, "RoleUpdate", nameof(Role), role.Id.ToString(),
            $"Updated role '{role.Name}'.", cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Role updated successfully." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.RolesWrite)]
    public async Task<IActionResult> DeleteRole(int id, CancellationToken cancellationToken)
    {
        var role = await _unitOfWork.Repository<Role>().Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Role not found.");

        var hasUsers = await _unitOfWork.Repository<UserRole>().Query().AnyAsync(x => x.RoleId == id, cancellationToken);
        if (hasUsers)
        {
            throw new ApiException(StatusCodes.Status409Conflict,
                "Role cannot be deleted because it is assigned to one or more users.");
        }

        var mappings = await _unitOfWork.Repository<RolePermission>().Query().Where(x => x.RoleId == id).ToListAsync(cancellationToken);
        _unitOfWork.Repository<RolePermission>().RemoveRange(mappings);
        _unitOfWork.Repository<Role>().Remove(role);

        await RecordAuditLogAsync(_auditLogService, "RoleDelete", nameof(Role), role.Id.ToString(),
            $"Deleted role '{role.Name}'.", cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Role deleted successfully." });
    }

    [HttpGet("permissions")]
    [Authorize(Policy = Permissions.RolesRead)]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        var permissions = await _unitOfWork.Repository<Permission>().Query()
            .AsNoTracking()
            .OrderBy(x => x.Module)
            .ThenBy(x => x.Action)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Module,
                x.Action,
                x.Description
            })
            .ToListAsync(cancellationToken);

        return Ok(permissions);
    }

    [HttpPut("{id:int}/permissions")]
    [Authorize(Policy = Permissions.RolesWrite)]
    public async Task<IActionResult> AssignPermissions(int id, [FromBody] AssignPermissionsRequest request, CancellationToken cancellationToken)
    {
        var role = await _unitOfWork.Repository<Role>().Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Role not found.");

        var permissions = await _unitOfWork.Repository<Permission>().Query()
            .Where(x => request.PermissionIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (request.PermissionIds.Count > 0 && permissions.Count != request.PermissionIds.Count)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "One or more permission IDs are invalid.");
        }

        var mappings = await _unitOfWork.Repository<RolePermission>().Query().Where(x => x.RoleId == id).ToListAsync(cancellationToken);
        _unitOfWork.Repository<RolePermission>().RemoveRange(mappings);
        await _unitOfWork.Repository<RolePermission>().AddRangeAsync(permissions.Select(x => new RolePermission { RoleId = id, PermissionId = x.Id }), cancellationToken);

        role.UpdatedBy = User.Identity?.Name;
        role.UpdatedDate = DateTime.UtcNow;

        await RecordAuditLogAsync(_auditLogService, "RolePermissionsAssign", nameof(Role), role.Id.ToString(),
            $"Assigned {permissions.Count} permission(s) to role '{role.Name}'.", cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Permissions assigned successfully." });
    }
}

public class UpsertRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AssignPermissionsRequest
{
    public List<int> PermissionIds { get; set; } = [];
}