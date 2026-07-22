using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Infrastructure.Authentication;
using EmployeeManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UsersController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.UsersRead)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .OrderBy(x => x.UserName)
            .Select(x => new
            {
                x.Id,
                x.UserName,
                x.Email,
                x.FirstName,
                x.LastName,
                x.IsActive,
                x.LastLoginAtUtc,
                roles = x.UserRoles.Select(ur => ur.Role.Name).ToArray()
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.UsersWrite)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserName)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.FirstName)
            || string.IsNullOrWhiteSpace(request.LastName)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Username, email, names, and password are required.");
        }

        if (!PasswordPolicyValidator.IsValid(request.Password))
        {
            throw new ApiException(StatusCodes.Status400BadRequest,
                "Password must be at least 8 characters and include uppercase, lowercase, number, and special character.");
        }

        var userNameExists = await _context.Users.AnyAsync(x => x.UserName == request.UserName, cancellationToken);
        if (userNameExists)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Username already exists.");
        }

        var emailExists = await _context.Users.AnyAsync(x => x.Email == request.Email, cancellationToken);
        if (emailExists)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Email already exists.");
        }

        var roles = await _context.Roles
            .Where(x => request.RoleIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (request.RoleIds.Count > 0 && roles.Count != request.RoleIds.Count)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "One or more role IDs are invalid.");
        }

        var user = new User
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IsActive = request.IsActive,
            PasswordChangedAtUtc = DateTime.UtcNow,
            PasswordExpiresAtUtc = DateTime.UtcNow.AddDays(90),
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        if (roles.Count > 0)
        {
            _context.UserRoles.AddRange(roles.Select(x => new UserRole { UserId = user.Id, RoleId = x.Id }));
            await _context.SaveChangesAsync(cancellationToken);
        }

        return CreatedAtAction(nameof(GetUsers), new { version = "1" }, user.Id);
    }

    [HttpPut("{id:int}/roles")]
    [Authorize(Policy = Permissions.UsersWrite)]
    public async Task<IActionResult> AssignRoles(int id, [FromBody] AssignRolesRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found.");

        var roles = await _context.Roles.Where(x => request.RoleIds.Contains(x.Id)).ToListAsync(cancellationToken);
        if (request.RoleIds.Count > 0 && roles.Count != request.RoleIds.Count)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "One or more role IDs are invalid.");
        }

        var existingMappings = await _context.UserRoles.Where(x => x.UserId == id).ToListAsync(cancellationToken);
        _context.UserRoles.RemoveRange(existingMappings);
        _context.UserRoles.AddRange(roles.Select(x => new UserRole { UserId = id, RoleId = x.Id }));

        user.UpdatedBy = User.Identity?.Name;
        user.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Roles assigned successfully." });
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Policy = Permissions.UsersWrite)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found.");

        user.IsActive = request.IsActive;
        user.UpdatedBy = User.Identity?.Name;
        user.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "User status updated successfully." });
    }
}

public class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<int> RoleIds { get; set; } = [];
}

public class AssignRolesRequest
{
    public List<int> RoleIds { get; set; } = [];
}

public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}