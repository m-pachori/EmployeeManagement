namespace EmployeeManagement.Application.Authentication.Dtos;

public class ResetPasswordRequest
{
    public string UserNameOrEmail { get; set; } = string.Empty;

    public string ResetToken { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}