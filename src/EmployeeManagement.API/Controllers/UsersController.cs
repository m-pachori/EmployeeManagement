using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Infrastructure.Authentication;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UsersController(IUnitOfWork unitOfWork, IPasswordHasher<User> passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.UsersRead)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.Repository<User>().Query()
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

        var userNameExists = await _unitOfWork.Repository<User>().Query().AnyAsync(x => x.UserName == request.UserName, cancellationToken);
        if (userNameExists)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Username already exists.");
        }

        var emailExists = await _unitOfWork.Repository<User>().Query().AnyAsync(x => x.Email == request.Email, cancellationToken);
        if (emailExists)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Email already exists.");
        }

        var roles = await _unitOfWork.Repository<Role>().Query()
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

        await _unitOfWork.Repository<User>().AddAsync(user, cancellationToken);
        await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserId = GetCurrentUserId(),
            EventType = "UserCreate",
            EntityName = nameof(User),
            Details = $"Created user '{user.UserName}'.",
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (roles.Count > 0)
        {
            await _unitOfWork.Repository<UserRole>().AddRangeAsync(
                roles.Select(x => new UserRole { UserId = user.Id, RoleId = x.Id }),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return CreatedAtAction(nameof(GetUsers), new { version = "1" }, user.Id);
    }

    [HttpPut("{id:int}/roles")]
    [Authorize(Policy = Permissions.UsersWrite)]
    public async Task<IActionResult> AssignRoles(int id, [FromBody] AssignRolesRequest request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Repository<User>().Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found.");

        var roles = await _unitOfWork.Repository<Role>().Query().Where(x => request.RoleIds.Contains(x.Id)).ToListAsync(cancellationToken);
        if (request.RoleIds.Count > 0 && roles.Count != request.RoleIds.Count)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "One or more role IDs are invalid.");
        }

        var existingMappings = await _unitOfWork.Repository<UserRole>().Query().Where(x => x.UserId == id).ToListAsync(cancellationToken);
        _unitOfWork.Repository<UserRole>().RemoveRange(existingMappings);
        await _unitOfWork.Repository<UserRole>().AddRangeAsync(roles.Select(x => new UserRole { UserId = id, RoleId = x.Id }), cancellationToken);

        user.UpdatedBy = User.Identity?.Name;
        user.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserId = GetCurrentUserId(),
            EventType = "UserRolesAssign",
            EntityName = nameof(User),
            EntityId = user.Id.ToString(),
            Details = $"Assigned {roles.Count} role(s) to user '{user.UserName}'.",
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Roles assigned successfully." });
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Policy = Permissions.UsersWrite)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Repository<User>().Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found.");

        user.IsActive = request.IsActive;
        user.UpdatedBy = User.Identity?.Name;
        user.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserId = GetCurrentUserId(),
            EventType = "UserStatusUpdate",
            EntityName = nameof(User),
            EntityId = user.Id.ToString(),
            Details = $"Set user '{user.UserName}' status to {(request.IsActive ? "Active" : "Inactive")}.",
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "User status updated successfully." });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.UsersWrite)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Repository<User>().Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found.");

        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.FirstName)
            || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Email, first name, and last name are required.");
        }

        var emailExists = await _unitOfWork.Repository<User>().Query().AnyAsync(x => x.Email == request.Email && x.Id != id, cancellationToken);
        if (emailExists)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Email already exists.");
        }

        user.Email = request.Email.Trim();
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.UpdatedBy = User.Identity?.Name;
        user.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserId = GetCurrentUserId(),
            EventType = "UserUpdate",
            EntityName = nameof(User),
            EntityId = user.Id.ToString(),
            Details = $"Updated user '{user.UserName}'.",
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "User updated successfully." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.UsersWrite)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Repository<User>().Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found.");

        var currentUserId = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (currentUserId is not null && int.TryParse(currentUserId.Value, out var loggedInUserId) && loggedInUserId == id)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "You cannot delete your own account.");
        }

        _unitOfWork.Repository<User>().Remove(user);
        await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserId = GetCurrentUserId(),
            EventType = "UserDelete",
            EntityName = nameof(User),
            EntityId = user.Id.ToString(),
            Details = $"Deleted user '{user.UserName}'.",
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "User deleted successfully." });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out var userId) ? userId : null;
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

public class UpdateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}