namespace EmployeeManagement.Application.Authentication.Dtos;

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}