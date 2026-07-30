using System.ComponentModel.DataAnnotations;
using EmployeeManagement.Domain.Enums;

namespace EmployeeManagement.Application.Employees.Dtos;

public class CreateEmployeeRequest
{
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    public string? Designation { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Salary must be a positive value.")]
    public decimal? Salary { get; set; }

    [Required]
    public DateTime DateOfJoining { get; set; }

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    [Required]
    public int DepartmentId { get; set; }

    public int? ManagerId { get; set; }
}
