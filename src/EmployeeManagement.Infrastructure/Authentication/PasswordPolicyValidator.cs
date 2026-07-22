using System.Text.RegularExpressions;

namespace EmployeeManagement.Infrastructure.Authentication;

public static partial class PasswordPolicyValidator
{
    public static bool IsValid(string password)
    {
        return !string.IsNullOrWhiteSpace(password)
               && password.Length >= 8
               && UppercaseRegex().IsMatch(password)
               && LowercaseRegex().IsMatch(password)
               && NumberRegex().IsMatch(password)
               && SpecialCharRegex().IsMatch(password);
    }

    [GeneratedRegex("[A-Z]")]
    private static partial Regex UppercaseRegex();

    [GeneratedRegex("[a-z]")]
    private static partial Regex LowercaseRegex();

    [GeneratedRegex("[0-9]")]
    private static partial Regex NumberRegex();

    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex SpecialCharRegex();
}