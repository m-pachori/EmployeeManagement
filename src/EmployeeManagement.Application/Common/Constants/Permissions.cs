namespace EmployeeManagement.Application.Common.Constants;

public static class Permissions
{
    public const string EmployeesRead = "Employees.Read";
    public const string EmployeesWrite = "Employees.Write";
    public const string DepartmentsRead = "Departments.Read";
    public const string DepartmentsWrite = "Departments.Write";
    public const string UsersRead = "Users.Read";
    public const string UsersWrite = "Users.Write";
    public const string RolesRead = "Roles.Read";
    public const string RolesWrite = "Roles.Write";
    public const string SettingsRead = "Settings.Read";
    public const string SettingsWrite = "Settings.Write";
    public const string ReportsRead = "Reports.Read";
    public const string DashboardRead = "Dashboard.Read";
    public const string AuditRead = "Audit.Read";

    public static readonly string[] All =
    [
        EmployeesRead,
        EmployeesWrite,
        DepartmentsRead,
        DepartmentsWrite,
        UsersRead,
        UsersWrite,
        RolesRead,
        RolesWrite,
        SettingsRead,
        SettingsWrite,
        ReportsRead,
        DashboardRead,
        AuditRead
    ];
}