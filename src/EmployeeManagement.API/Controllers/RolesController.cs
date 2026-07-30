using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Roles.Dtos;
using EmployeeManagement.Application.Roles.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
[Authorize]
public class RolesController : ApiControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.RolesRead)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetAllAsync(cancellationToken);
        return Ok(roles);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.RolesWrite)]
    public async Task<IActionResult> CreateRole([FromBody] UpsertRoleRequest request, CancellationToken cancellationToken)
    {
        var id = await _roleService.CreateAsync(request, CurrentUserName ?? "system", cancellationToken);
        return CreatedAtAction(nameof(GetRoles), new { version = "1" }, id);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.RolesWrite)]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpsertRoleRequest request, CancellationToken cancellationToken)
    {
        await _roleService.UpdateAsync(id, request, CurrentUserName ?? "system", cancellationToken);
        return Ok(new { message = "Role updated successfully." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.RolesWrite)]
    public async Task<IActionResult> DeleteRole(int id, CancellationToken cancellationToken)
    {
        await _roleService.DeleteAsync(id, CurrentUserName ?? "system", cancellationToken);
        return Ok(new { message = "Role deleted successfully." });
    }

    [HttpGet("permissions")]
    [Authorize(Policy = Permissions.RolesRead)]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        var permissions = await _roleService.GetPermissionsAsync(cancellationToken);
        return Ok(permissions);
    }

    [HttpPut("{id:int}/permissions")]
    [Authorize(Policy = Permissions.RolesWrite)]
    public async Task<IActionResult> AssignPermissions(int id, [FromBody] AssignPermissionsRequest request, CancellationToken cancellationToken)
    {
        await _roleService.AssignPermissionsAsync(id, request, CurrentUserName ?? "system", cancellationToken);
        return Ok(new { message = "Permissions assigned successfully." });
    }
}
