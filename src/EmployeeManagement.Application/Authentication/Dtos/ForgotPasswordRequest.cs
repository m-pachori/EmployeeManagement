namespace EmployeeManagement.Application.Authentication.Dtos;

public class ForgotPasswordRequest
{
    public string UserNameOrEmail { get; set; } = string.Empty;
}