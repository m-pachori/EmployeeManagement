using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Application.Departments.Dtos;

public class UpsertDepartmentRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
