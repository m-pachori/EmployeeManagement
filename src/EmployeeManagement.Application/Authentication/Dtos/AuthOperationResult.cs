namespace EmployeeManagement.Application.Authentication.Dtos;

// NOTE: This DTO is serialized directly in API responses (see AuthController).
// Never add a raw secret/token property here (CWE-640) — reset tokens must only
// ever be delivered out-of-band (email/SMS), never echoed back over HTTP.
public class AuthOperationResult
{
    public bool Succeeded { get; set; }

    public string Message { get; set; } = string.Empty;

    public static AuthOperationResult Success(string message)
    {
        return new AuthOperationResult { Succeeded = true, Message = message };
    }

    public static AuthOperationResult Failure(string message)
    {
        return new AuthOperationResult { Succeeded = false, Message = message };
    }
}