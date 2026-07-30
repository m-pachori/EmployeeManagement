using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeManagement.Application.Authentication.Dtos;
using EmployeeManagement.Application.Authentication.Interfaces;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtOptions _jwtOptions;
    private readonly AuthOptions _authOptions;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AuthService> _logger;
    private readonly IAuditLogService _auditLogService;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher,
        IOptions<JwtOptions> jwtOptions,
        IOptions<AuthOptions> authOptions,
        IHostEnvironment environment,
        ILogger<AuthService> logger,
        IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtOptions = jwtOptions.Value;
        _authOptions = authOptions.Value;
        _environment = environment;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string clientIp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserNameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ApiException(BadRequest, "Username/email and password are required.");
        }

        var normalizedValue = request.UserNameOrEmail.Trim().ToUpperInvariant();

        var user = await _unitOfWork.Repository<User>().Query()
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

            await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        await _auditLogService.RecordAsync("Login", nameof(User), user.Id.ToString(),
            "User logged in successfully.", user.Id, user.UserName, clientIp, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BuildLoginResponse(user, refreshTokenValue, refreshTokenExpiresAt);
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, string clientIp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new ApiException(BadRequest, "Refresh token is required.");
        }

        var tokenHash = TokenGenerator.HashToken(request.RefreshToken);

        var refreshToken = await _unitOfWork.Repository<RefreshToken>().Query()
            .Include(x => x.User)
                .ThenInclude(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .Include(x => x.User)
                .ThenInclude(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .ThenInclude(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            throw new ApiException(Unauthorized, "Invalid refresh token.");
        }

        if (refreshToken.IsRevoked)
        {
            // SECURITY: reuse of an already-rotated/revoked refresh token is a strong signal
            // of token theft. Revoke every active refresh token for this user so a stolen
            // token (e.g. exfiltrated from localStorage via XSS) can't keep a session alive,
            // and force re-authentication on all devices.
            var activeTokens = await _unitOfWork.Repository<RefreshToken>().Query()
                .Where(x => x.UserId == refreshToken.UserId && x.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var activeToken in activeTokens)
            {
                activeToken.RevokedAtUtc = DateTime.UtcNow;
                activeToken.RevokedByIp = clientIp;
            }

            await _auditLogService.RecordAsync("RefreshTokenReuseDetected", nameof(User), refreshToken.UserId.ToString(),
                "Reuse of a revoked refresh token was detected; all active sessions for this user were revoked.",
                refreshToken.UserId, actorName: null, ipAddress: clientIp, cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Refresh token reuse detected for user {UserId} from {ClientIp}; all sessions revoked.", refreshToken.UserId, clientIp);

            throw new ApiException(Unauthorized, "Invalid refresh token.");
        }

        if (refreshToken.IsExpired)
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BuildLoginResponse(user, newRefreshTokenValue, newRefreshTokenExpiresAt);
    }

    public async Task<AuthOperationResult> LogoutAsync(LogoutRequest request, string clientIp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return AuthOperationResult.Success("Logout completed.");
        }

        var tokenHash = TokenGenerator.HashToken(request.RefreshToken);

        var refreshToken = await _unitOfWork.Repository<RefreshToken>().Query()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null || refreshToken.IsRevoked)
        {
            return AuthOperationResult.Success("Logout completed.");
        }

        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        refreshToken.RevokedByIp = clientIp;

        await _auditLogService.RecordAsync("Logout", nameof(User), refreshToken.UserId.ToString(),
            "User logged out.", refreshToken.UserId, refreshToken.User.UserName, clientIp, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthOperationResult.Success("Logout completed.");
    }

    public async Task<AuthOperationResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedValue = request.UserNameOrEmail.Trim().ToUpperInvariant();
        var user = await _unitOfWork.Repository<User>().Query()
            .FirstOrDefaultAsync(x => x.UserName.ToUpper() == normalizedValue || x.Email.ToUpper() == normalizedValue, cancellationToken);

        if (user is null)
        {
            return AuthOperationResult.Success("If the user exists, a reset token has been generated.");
        }

        var resetToken = TokenGenerator.GenerateRefreshToken();
        user.PasswordResetTokenHash = TokenGenerator.HashToken(resetToken);
        user.PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(_authOptions.ResetTokenMinutes);

        await _auditLogService.RecordAsync("ForgotPassword", nameof(User), user.Id.ToString(),
            "Password reset token generated.", user.Id, user.UserName, ipAddress: null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // SECURITY: never return the raw reset token in the API response (CWE-640) —
        // in a real deployment this must be delivered out-of-band via email/SMS using
        // the configured SMTP settings. Logged only in Development for local testing,
        // since this app has no email integration yet.
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation(
                "[DEV ONLY - never logged in production] Password reset token for user {UserId}: {ResetToken}",
                user.Id, resetToken);
        }

        return AuthOperationResult.Success("If the account exists, a password reset token has been generated and sent to the registered contact method.");
    }

    public async Task<AuthOperationResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedValue = request.UserNameOrEmail.Trim().ToUpperInvariant();
        var user = await _unitOfWork.Repository<User>().Query()
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

        await _auditLogService.RecordAsync("ResetPassword", nameof(User), user.Id.ToString(),
            "Password reset completed.", user.Id, user.UserName, ipAddress: null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthOperationResult.Success("Password has been reset.");
    }

    public async Task<AuthOperationResult> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().Query().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
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

        await _auditLogService.RecordAsync("ChangePassword", nameof(User), user.Id.ToString(),
            "Password changed by authenticated user.", user.Id, user.UserName, ipAddress: null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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