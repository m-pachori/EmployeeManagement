using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Users.Dtos;
using EmployeeManagement.Application.Users.Interfaces;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmployeeManagement.Infrastructure.Services;

/// <summary>
/// Application-layer service for User management operations.
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IAuditLogService _auditLogService;
    private readonly AuthOptions _authOptions;

    public UserService(
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher,
        IAuditLogService auditLogService,
        IOptions<AuthOptions> authOptions)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _auditLogService = auditLogService;
        _authOptions = authOptions.Value;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Repository<User>().Query()
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .OrderBy(x => x.UserName)
            .Select(x => new UserDto
            {
                Id = x.Id,
                UserName = x.UserName,
                Email = x.Email,
                FirstName = x.FirstName,
                LastName = x.LastName,
                IsActive = x.IsActive,
                LastLoginAtUtc = x.LastLoginAtUtc,
                Roles = x.UserRoles.Select(ur => ur.Role.Name).ToArray()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(CreateUserRequest request, string actorName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.FirstName)
            || string.IsNullOrWhiteSpace(request.LastName)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ApiException(400, "Username, email, names, and password are required.");
        }

        if (!PasswordPolicyValidator.IsValid(request.Password))
            throw new ApiException(400, "Password must be at least 8 characters and include uppercase, lowercase, number, and special character.");

        var userNameExists = await _unitOfWork.Repository<User>().Query()
            .AnyAsync(x => x.UserName == request.UserName, cancellationToken);
        if (userNameExists)
            throw new ApiException(409, "Username already exists.");

        var emailExists = await _unitOfWork.Repository<User>().Query()
            .AnyAsync(x => x.Email == request.Email, cancellationToken);
        if (emailExists)
            throw new ApiException(409, "Email already exists.");

        var roles = await _unitOfWork.Repository<Role>().Query()
            .Where(x => request.RoleIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (request.RoleIds.Count > 0 && roles.Count != request.RoleIds.Count)
            throw new ApiException(400, "One or more role IDs are invalid.");

        var user = new User
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IsActive = request.IsActive,
            PasswordChangedAtUtc = DateTime.UtcNow,
            // TD-09: use configured expiry days instead of the hardcoded 90-day literal
            PasswordExpiresAtUtc = DateTime.UtcNow.AddDays(_authOptions.PasswordExpiryDays),
            CreatedBy = actorName,
            UpdatedBy = actorName
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _unitOfWork.Repository<User>().AddAsync(user, cancellationToken);
        await _auditLogService.RecordAsync("UserCreate", nameof(User), null,
            $"Created user '{user.UserName}'.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (roles.Count > 0)
        {
            await _unitOfWork.Repository<UserRole>().AddRangeAsync(
                roles.Select(x => new UserRole { UserId = user.Id, RoleId = x.Id }),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return user.Id;
    }

    public async Task UpdateAsync(int id, UpdateUserRequest request, string actorName, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(404, "User not found.");

        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.FirstName)
            || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ApiException(400, "Email, first name, and last name are required.");
        }

        var emailExists = await _unitOfWork.Repository<User>().Query()
            .AnyAsync(x => x.Email == request.Email && x.Id != id, cancellationToken);
        if (emailExists)
            throw new ApiException(409, "Email already exists.");

        user.Email = request.Email.Trim();
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.UpdatedBy = actorName;
        user.UpdatedDate = DateTime.UtcNow;

        await _auditLogService.RecordAsync("UserUpdate", nameof(User), id.ToString(),
            $"Updated user '{user.UserName}'.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignRolesAsync(int id, AssignRolesRequest request, string actorName, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(404, "User not found.");

        var roles = await _unitOfWork.Repository<Role>().Query()
            .Where(x => request.RoleIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (request.RoleIds.Count > 0 && roles.Count != request.RoleIds.Count)
            throw new ApiException(400, "One or more role IDs are invalid.");

        var existingMappings = await _unitOfWork.Repository<UserRole>().Query()
            .Where(x => x.UserId == id)
            .ToListAsync(cancellationToken);
        _unitOfWork.Repository<UserRole>().RemoveRange(existingMappings);

        await _unitOfWork.Repository<UserRole>().AddRangeAsync(
            roles.Select(x => new UserRole { UserId = id, RoleId = x.Id }),
            cancellationToken);

        user.UpdatedBy = actorName;
        user.UpdatedDate = DateTime.UtcNow;

        await _auditLogService.RecordAsync("UserRolesAssign", nameof(User), id.ToString(),
            $"Assigned {roles.Count} role(s) to user '{user.UserName}'.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(int id, UpdateUserStatusRequest request, string actorName, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(404, "User not found.");

        user.IsActive = request.IsActive;
        user.UpdatedBy = actorName;
        user.UpdatedDate = DateTime.UtcNow;

        await _auditLogService.RecordAsync("UserStatusUpdate", nameof(User), id.ToString(),
            $"Set user '{user.UserName}' status to {(request.IsActive ? "Active" : "Inactive")}.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, int? currentUserId, string actorName, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(404, "User not found.");

        if (currentUserId.HasValue && currentUserId.Value == id)
            throw new ApiException(400, "You cannot delete your own account.");

        _unitOfWork.Repository<User>().Remove(user);
        await _auditLogService.RecordAsync("UserDelete", nameof(User), id.ToString(),
            $"Deleted user '{user.UserName}'.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
