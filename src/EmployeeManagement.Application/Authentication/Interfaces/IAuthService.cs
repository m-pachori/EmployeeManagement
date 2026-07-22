using EmployeeManagement.Application.Authentication.Dtos;

namespace EmployeeManagement.Application.Authentication.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string clientIp, CancellationToken cancellationToken = default);

    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, string clientIp, CancellationToken cancellationToken = default);

    Task<AuthOperationResult> LogoutAsync(LogoutRequest request, string clientIp, CancellationToken cancellationToken = default);

    Task<AuthOperationResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task<AuthOperationResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<AuthOperationResult> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}