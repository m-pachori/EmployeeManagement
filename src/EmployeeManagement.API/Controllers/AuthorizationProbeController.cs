using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.API.Controllers;

// ARCHITECTURE: this is a diagnostic-only controller used to verify the role/permission
// authorization pipeline is wired correctly. It has no business purpose and must never be
// reachable outside Development (previously shipped unconditionally - see technical debt
// review). Gated via IHostEnvironment rather than #if DEBUG so a Release build run with
// ASPNETCORE_ENVIRONMENT=Development still behaves the same as a local dev run.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/security")]
[Authorize]
public class AuthorizationProbeController : ControllerBase
{
    private readonly IHostEnvironment _environment;

    public AuthorizationProbeController(IHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("role-admin")]
    [Authorize(Roles = DefaultRoles.Admin)]
    public IActionResult AdminRoleOnly()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(new { message = "Role-based authorization passed." });
    }

    [HttpGet("permission-users-read")]
    [Authorize(Policy = Permissions.UsersRead)]
    public IActionResult UsersReadPolicy()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(new { message = "Permission-based authorization passed (Users.Read)." });
    }
}