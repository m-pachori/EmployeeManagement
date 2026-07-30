using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Users.Dtos;
using EmployeeManagement.Application.Users.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.UsersRead)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.UsersWrite)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var id = await _userService.CreateAsync(request, CurrentUserName ?? "system", cancellationToken);
        return CreatedAtAction(nameof(GetUsers), new { version = "1" }, id);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.UsersWrite)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await _userService.UpdateAsync(id, request, CurrentUserName ?? "system", cancellationToken);
        return Ok(new { message = "User updated successfully." });
    }

    [HttpPut("{id:int}/roles")]
    [Authorize(Policy = Permissions.UsersWrite)]
    public async Task<IActionResult> AssignRoles(int id, [FromBody] AssignRolesRequest request, CancellationToken cancellationToken)
    {
        await _userService.AssignRolesAsync(id, request, CurrentUserName ?? "system", cancellationToken);
        return Ok(new { message = "Roles assigned successfully." });
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Policy = Permissions.UsersWrite)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        await _userService.UpdateStatusAsync(id, request, CurrentUserName ?? "system", cancellationToken);
        return Ok(new { message = "User status updated successfully." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.UsersWrite)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        // TD-06: removed duplicate GetCurrentUserId() — now uses ApiControllerBase.GetCurrentUserId()
        await _userService.DeleteAsync(id, GetCurrentUserId(), CurrentUserName ?? "system", cancellationToken);
        return Ok(new { message = "User deleted successfully." });
    }
}
