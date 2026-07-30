namespace EmployeeManagement.Application.Roles.Dtos;

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PermissionCount { get; set; }
    public int UserCount { get; set; }
}
