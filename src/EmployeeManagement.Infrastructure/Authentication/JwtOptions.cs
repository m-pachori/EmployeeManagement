namespace EmployeeManagement.Infrastructure.Authentication;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "EmployeeManagement";

    public string Audience { get; set; } = "EmployeeManagement.Client";

    public string SecretKey { get; set; } = "THIS_IS_FOR_LOCAL_DEVELOPMENT_ONLY_CHANGE_IT";

    public int AccessTokenMinutes { get; set; } = 30;

    public int RefreshTokenDays { get; set; } = 7;
}