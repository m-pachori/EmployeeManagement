namespace EmployeeManagement.Infrastructure.Authentication;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public int MaxFailedAccessAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    public int PasswordExpiryDays { get; set; } = 90;

    public int ResetTokenMinutes { get; set; } = 30;
}