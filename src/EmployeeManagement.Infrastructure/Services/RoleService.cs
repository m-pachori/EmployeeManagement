using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Roles.Dtos;
using EmployeeManagement.Application.Roles.Interfaces;
using EmployeeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Infrastructure.Services;

/// <summary>
/// Application-layer service for Role and Permission management operations.
/// </summary>
public class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public RoleService(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Repository<Role>().Query()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new RoleDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                PermissionCount = x.RolePermissions.Count,
                UserCount = x.UserRoles.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(UpsertRoleRequest request, string actorName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ApiException(400, "Role name is required.");

        var exists = await _unitOfWork.Repository<Role>().Query()
            .AnyAsync(x => x.Name == request.Name, cancellationToken);
        if (exists)
            throw new ApiException(409, "Role already exists.");

        var role = new Role
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            CreatedBy = actorName,
            UpdatedBy = actorName
        };

        await _unitOfWork.Repository<Role>().AddAsync(role, cancellationToken);
        await _auditLogService.RecordAsync("RoleCreate", nameof(Role), null,
            $"Created role '{role.Name}'.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return role.Id;
    }

    public async Task UpdateAsync(int id, UpsertRoleRequest request, string actorName, CancellationToken cancellationToken = default)
    {
        var role = await _unitOfWork.Repository<Role>().Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Role not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ApiException(400, "Role name is required.");

        var duplicate = await _unitOfWork.Repository<Role>().Query()
            .AnyAsync(x => x.Name == request.Name && x.Id != id, cancellationToken);
        if (duplicate)
            throw new ApiException(409, "Role already exists.");

        role.Name = request.Name.Trim();
        role.Description = request.Description?.Trim() ?? string.Empty;
        role.UpdatedBy = actorName;
        role.UpdatedDate = DateTime.UtcNow;

        await _auditLogService.RecordAsync("RoleUpdate", nameof(Role), id.ToString(),
            $"Updated role '{role.Name}'.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, string actorName, CancellationToken cancellationToken = default)
    {
        var role = await _unitOfWork.Repository<Role>().Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Role not found.");

        var hasUsers = await _unitOfWork.Repository<UserRole>().Query()
            .AnyAsync(x => x.RoleId == id, cancellationToken);
        if (hasUsers)
            throw new ApiException(409, "Role cannot be deleted because it is assigned to one or more users.");

        var mappings = await _unitOfWork.Repository<RolePermission>().Query()
            .Where(x => x.RoleId == id)
            .ToListAsync(cancellationToken);
        _unitOfWork.Repository<RolePermission>().RemoveRange(mappings);
        _unitOfWork.Repository<Role>().Remove(role);

        await _auditLogService.RecordAsync("RoleDelete", nameof(Role), id.ToString(),
            $"Deleted role '{role.Name}'.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Repository<Permission>().Query()
            .AsNoTracking()
            .OrderBy(x => x.Module)
            .ThenBy(x => x.Action)
            .Select(x => new PermissionDto
            {
                Id = x.Id,
                Code = x.Code,
                Module = x.Module,
                Action = x.Action,
                Description = x.Description
            })
            .ToListAsync(cancellationToken);
    }

    public async Task AssignPermissionsAsync(int id, AssignPermissionsRequest request, string actorName, CancellationToken cancellationToken = default)
    {
        var role = await _unitOfWork.Repository<Role>().Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Role not found.");

        var permissions = await _unitOfWork.Repository<Permission>().Query()
            .Where(x => request.PermissionIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (request.PermissionIds.Count > 0 && permissions.Count != request.PermissionIds.Count)
            throw new ApiException(400, "One or more permission IDs are invalid.");

        var mappings = await _unitOfWork.Repository<RolePermission>().Query()
            .Where(x => x.RoleId == id)
            .ToListAsync(cancellationToken);
        _unitOfWork.Repository<RolePermission>().RemoveRange(mappings);

        await _unitOfWork.Repository<RolePermission>().AddRangeAsync(
            permissions.Select(x => new RolePermission { RoleId = id, PermissionId = x.Id }),
            cancellationToken);

        role.UpdatedBy = actorName;
        role.UpdatedDate = DateTime.UtcNow;

        await _auditLogService.RecordAsync("RolePermissionsAssign", nameof(Role), id.ToString(),
            $"Assigned {permissions.Count} permission(s) to role '{role.Name}'.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
