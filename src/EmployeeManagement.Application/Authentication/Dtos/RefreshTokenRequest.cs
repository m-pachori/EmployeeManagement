namespace EmployeeManagement.Application.Authentication.Dtos;

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}