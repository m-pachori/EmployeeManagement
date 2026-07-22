namespace EmployeeManagement.Application.Authentication.Dtos;

public class AuthOperationResult
{
    public bool Succeeded { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ResetToken { get; set; }

    public static AuthOperationResult Success(string message, string? resetToken = null)
    {
        return new AuthOperationResult { Succeeded = true, Message = message, ResetToken = resetToken };
    }

    public static AuthOperationResult Failure(string message)
    {
        return new AuthOperationResult { Succeeded = false, Message = message };
    }
}