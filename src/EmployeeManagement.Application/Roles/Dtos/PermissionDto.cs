namespace EmployeeManagement.Application.Roles.Dtos;

public class PermissionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
