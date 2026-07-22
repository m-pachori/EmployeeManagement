using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RolesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.RolesRead)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
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

        var exists = await _context.Roles.AnyAsync(x => x.Name == request.Name, cancellationToken);
        if (exists)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Role already exists.");
        }

        var role = new Role
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetRoles), new { version = "1" }, role.Id);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.RolesWrite)]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpsertRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Role not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Role name is required.");
        }

        var duplicate = await _context.Roles.AnyAsync(x => x.Name == request.Name && x.Id != id, cancellationToken);
        if (duplicate)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Role already exists.");
        }

        role.Name = request.Name.Trim();
        role.Description = request.Description.Trim();
        role.UpdatedBy = User.Identity?.Name;
        role.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Role updated successfully." });
    }

    [HttpGet("permissions")]
    [Authorize(Policy = Permissions.RolesRead)]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        var permissions = await _context.Permissions
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
        var role = await _context.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Role not found.");

        var permissions = await _context.Permissions
            .Where(x => request.PermissionIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (request.PermissionIds.Count > 0 && permissions.Count != request.PermissionIds.Count)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "One or more permission IDs are invalid.");
        }

        var mappings = await _context.RolePermissions.Where(x => x.RoleId == id).ToListAsync(cancellationToken);
        _context.RolePermissions.RemoveRange(mappings);
        _context.RolePermissions.AddRange(permissions.Select(x => new RolePermission { RoleId = id, PermissionId = x.Id }));

        role.UpdatedBy = User.Identity?.Name;
        role.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
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