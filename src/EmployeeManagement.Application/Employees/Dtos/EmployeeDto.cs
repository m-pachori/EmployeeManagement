namespace EmployeeManagement.Application.Employees.Dtos;

public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Designation { get; set; }
    public decimal? Salary { get; set; }
    public DateTime DateOfJoining { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string Department { get; set; } = string.Empty;
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public DateTime CreatedDate { get; set; }
}
