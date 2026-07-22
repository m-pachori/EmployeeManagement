using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/security")]
[Authorize]
public class AuthorizationProbeController : ControllerBase
{
    [HttpGet("role-admin")]
    [Authorize(Roles = DefaultRoles.Admin)]
    public IActionResult AdminRoleOnly()
    {
        return Ok(new { message = "Role-based authorization passed." });
    }

    [HttpGet("permission-users-read")]
    [Authorize(Policy = Permissions.UsersRead)]
    public IActionResult UsersReadPolicy()
    {
        return Ok(new { message = "Permission-based authorization passed (Users.Read)." });
    }
}