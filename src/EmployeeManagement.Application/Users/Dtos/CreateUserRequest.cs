using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Application.Users.Dtos;

public class CreateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<int> RoleIds { get; set; } = [];
}
