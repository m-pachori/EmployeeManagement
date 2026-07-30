using EmployeeManagement.Domain.Enums;

namespace EmployeeManagement.Application.Employees.Dtos;

public class EmployeeListRequest
{
    public string? Search { get; set; }
    public int? DepartmentId { get; set; }
    public EmployeeStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "createdDate";
    public string SortDirection { get; set; } = "desc";
}
