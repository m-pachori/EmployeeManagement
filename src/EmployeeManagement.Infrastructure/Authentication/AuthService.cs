using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeManagement.Application.Authentication.Dtos;
using EmployeeManagement.Application.Authentication.Interfaces;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManagement.Infrastructure.Authentication;

public class AuthService : IAuthService
{
    private const int BadRequest = 400;
    private const int Unauthorized = 401;
    private const int Forbidden = 403;
    private const int NotFound = 404;
    private const int Locked = 423;

    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtOptions _jwtOptions;
    private readonly AuthOptions _authOptions;

    public AuthService(
        ApplicationDbContext context,
        IPasswordHasher<User> passwordHasher,
        IOptions<JwtOptions> jwtOptions,
        IOptions<AuthOptions> authOptions)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtOptions = jwtOptions.Value;
        _authOptions = authOptions.Value;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string clientIp, CancellationToken cancellationToken = default)
    {
        var normalizedValue = request.UserNameOrEmail.Trim().ToUpperInvariant();

        var user = await _context.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .ThenInclude(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)
            .Include(x => x.RefreshTokens)
            .FirstOrDefaultAsync(x => x.UserName.ToUpper() == normalizedValue || x.Email.ToUpper() == normalizedValue, cancellationToken);

        if (user is null)
        {
            throw new ApiException(Unauthorized, "Invalid username/email or password.");
        }

        if (!user.IsActive)
        {
            throw new ApiException(Forbidden, "User account is inactive.");
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            throw new ApiException(Locked,
                $"Account is locked until {user.LockoutEndUtc.Value:u}.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= _authOptions.MaxFailedAccessAttempts)
            {
                user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(_authOptions.LockoutMinutes);
                user.FailedLoginAttempts = 0;
            }

            await _context.SaveChangesAsync(cancellationToken);

            throw new ApiException(Unauthorized, "Invalid username/email or password.");
        }

        if (user.PasswordExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new ApiException(Forbidden, "Password has expired. Please reset or change your password.");
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.LastLoginAtUtc = DateTime.UtcNow;

        var refreshTokenValue = TokenGenerator.GenerateRefreshToken();
        var refreshTokenHash = TokenGenerator.HashToken(refreshTokenValue);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

        user.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = refreshTokenHash,
            ExpiresAtUtc = refreshTokenExpiresAt,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = clientIp
        });

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            EventType = "Login",
            EntityName = nameof(User),
            EntityId = user.Id.ToString(),
            Details = "User logged in successfully.",
            IpAddress = clientIp,
            CreatedBy = user.UserName,
            UpdatedBy = user.UserName
        });

        await _context.SaveChangesAsync(cancellationToken);

        return BuildLoginResponse(user, refreshTokenValue, refreshTokenExpiresAt);
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, string clientIp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new ApiException(BadRequest, "Refresh token is required.");
        }

        var tokenHash = TokenGenerator.HashToken(request.RefreshToken);

        var refreshToken = await _context.RefreshTokens
            .Include(x => x.User)
                .ThenInclude(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .Include(x => x.User)
                .ThenInclude(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .ThenInclude(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null || refreshToken.IsExpired || refreshToken.IsRevoked)
        {
            throw new ApiException(Unauthorized, "Invalid refresh token.");
        }

        var user = refreshToken.User;
        if (!user.IsActive)
        {
            throw new ApiException(Forbidden, "User account is inactive.");
        }

        var newRefreshTokenValue = TokenGenerator.GenerateRefreshToken();
        var newRefreshTokenHash = TokenGenerator.HashToken(newRefreshTokenValue);
        var newRefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        refreshToken.RevokedByIp = clientIp;
        refreshToken.ReplacedByTokenHash = newRefreshTokenHash;

        user.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = newRefreshTokenHash,
            ExpiresAtUtc = newRefreshTokenExpiresAt,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = clientIp
        });

        await _context.SaveChangesAsync(cancellationToken);

        return BuildLoginResponse(user, newRefreshTokenValue, newRefreshTokenExpiresAt);
    }

    public async Task<AuthOperationResult> LogoutAsync(LogoutRequest request, string clientIp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return AuthOperationResult.Success("Logout completed.");
        }

        var tokenHash = TokenGenerator.HashToken(request.RefreshToken);

        var refreshToken = await _context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null || refreshToken.IsRevoked)
        {
            return AuthOperationResult.Success("Logout completed.");
        }

        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        refreshToken.RevokedByIp = clientIp;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = refreshToken.UserId,
            EventType = "Logout",
            EntityName = nameof(User),
            EntityId = refreshToken.UserId.ToString(),
            Details = "User logged out.",
            IpAddress = clientIp,
            CreatedBy = refreshToken.User.UserName,
            UpdatedBy = refreshToken.User.UserName
        });

        await _context.SaveChangesAsync(cancellationToken);

        return AuthOperationResult.Success("Logout completed.");
    }

    public async Task<AuthOperationResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedValue = request.UserNameOrEmail.Trim().ToUpperInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.UserName.ToUpper() == normalizedValue || x.Email.ToUpper() == normalizedValue, cancellationToken);

        if (user is null)
        {
            return AuthOperationResult.Success("If the user exists, a reset token has been generated.");
        }

        var resetToken = TokenGenerator.GenerateRefreshToken();
        user.PasswordResetTokenHash = TokenGenerator.HashToken(resetToken);
        user.PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(_authOptions.ResetTokenMinutes);

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            EventType = "ForgotPassword",
            EntityName = nameof(User),
            EntityId = user.Id.ToString(),
            Details = "Password reset token generated.",
            CreatedBy = user.UserName,
            UpdatedBy = user.UserName
        });

        await _context.SaveChangesAsync(cancellationToken);

        return AuthOperationResult.Success("Password reset token generated.", resetToken);
    }

    public async Task<AuthOperationResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedValue = request.UserNameOrEmail.Trim().ToUpperInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.UserName.ToUpper() == normalizedValue || x.Email.ToUpper() == normalizedValue, cancellationToken);

        if (user is null)
        {
            throw new ApiException(BadRequest, "Invalid reset request.");
        }

        if (!PasswordPolicyValidator.IsValid(request.NewPassword))
        {
            throw new ApiException(BadRequest,
                "Password must be at least 8 characters and include uppercase, lowercase, number, and special character.");
        }

        if (string.IsNullOrWhiteSpace(request.ResetToken)
            || string.IsNullOrWhiteSpace(user.PasswordResetTokenHash)
            || !string.Equals(TokenGenerator.HashToken(request.ResetToken), user.PasswordResetTokenHash, StringComparison.Ordinal)
            || !user.PasswordResetTokenExpiresAtUtc.HasValue
            || user.PasswordResetTokenExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            throw new ApiException(BadRequest, "Invalid or expired reset token.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.PasswordChangedAtUtc = DateTime.UtcNow;
        user.PasswordExpiresAtUtc = DateTime.UtcNow.AddDays(_authOptions.PasswordExpiryDays);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAtUtc = null;
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            EventType = "ResetPassword",
            EntityName = nameof(User),
            EntityId = user.Id.ToString(),
            Details = "Password reset completed.",
            CreatedBy = user.UserName,
            UpdatedBy = user.UserName
        });

        await _context.SaveChangesAsync(cancellationToken);

        return AuthOperationResult.Success("Password has been reset.");
    }

    public async Task<AuthOperationResult> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new ApiException(NotFound, "User not found.");

        if (!PasswordPolicyValidator.IsValid(request.NewPassword))
        {
            throw new ApiException(BadRequest,
                "Password must be at least 8 characters and include uppercase, lowercase, number, and special character.");
        }

        var verifyCurrent = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verifyCurrent == PasswordVerificationResult.Failed)
        {
            throw new ApiException(BadRequest, "Current password is incorrect.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.PasswordChangedAtUtc = DateTime.UtcNow;
        user.PasswordExpiresAtUtc = DateTime.UtcNow.AddDays(_authOptions.PasswordExpiryDays);

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            EventType = "ChangePassword",
            EntityName = nameof(User),
            EntityId = user.Id.ToString(),
            Details = "Password changed by authenticated user.",
            CreatedBy = user.UserName,
            UpdatedBy = user.UserName
        });

        await _context.SaveChangesAsync(cancellationToken);

        return AuthOperationResult.Success("Password changed successfully.");
    }

    private LoginResponse BuildLoginResponse(User user, string refreshTokenValue, DateTime refreshTokenExpiresAt)
    {
        var roles = user.UserRoles
            .Select(x => x.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var permissions = user.UserRoles
            .SelectMany(x => x.Role.RolePermissions)
            .Select(x => x.Permission.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var accessToken = GenerateAccessToken(user, roles, permissions, accessTokenExpiry);

        return new LoginResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessTokenExpiry,
            RefreshToken = refreshTokenValue,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAt,
            UserName = user.UserName,
            Email = user.Email,
            Roles = roles,
            Permissions = permissions
        };
    }

    private string GenerateAccessToken(User user, IReadOnlyList<string> roles, IReadOnlyList<string> permissions, DateTime expiresAtUtc)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}