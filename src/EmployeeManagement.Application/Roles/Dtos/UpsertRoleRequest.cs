using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Application.Roles.Dtos;

public class UpsertRoleRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}
