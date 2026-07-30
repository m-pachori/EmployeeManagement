using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Employees.Dtos;
using EmployeeManagement.Application.Employees.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees")]
[Authorize]
public class EmployeesController : ApiControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IWebHostEnvironment _environment;

    public EmployeesController(IEmployeeService employeeService, IWebHostEnvironment environment)
    {
        _employeeService = employeeService;
        _environment = environment;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.EmployeesRead)]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] string? search,
        [FromQuery] int? departmentId,
        [FromQuery] Domain.Enums.EmployeeStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "createdDate",
        [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = ClampPagination(page, pageSize);

        var result = await _employeeService.GetEmployeesAsync(new EmployeeListRequest
        {
            Search = search,
            DepartmentId = departmentId,
            Status = status,
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.EmployeesRead)]
    public async Task<IActionResult> GetEmployeeById(int id, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdAsync(id, cancellationToken);
        return Ok(employee);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EmployeesWrite)]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var id = await _employeeService.CreateAsync(request, CurrentUserName ?? "system", cancellationToken);
        return CreatedAtAction(nameof(GetEmployeeById), new { id, version = "1" }, id);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.EmployeesWrite)]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        await _employeeService.UpdateAsync(id, request, CurrentUserName ?? "system", cancellationToken);
        return Ok(new { message = "Employee updated successfully." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.EmployeesWrite)]
    public async Task<IActionResult> DeleteEmployee(int id, CancellationToken cancellationToken)
    {
        await _employeeService.DeleteAsync(id, CurrentUserName ?? "system", cancellationToken);
        return Ok(new { message = "Employee deleted successfully." });
    }

    [HttpPost("{id:int}/photo")]
    [Authorize(Policy = Permissions.EmployeesWrite)]
    [RequestSizeLimit(256 * 1024)]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Photo file is required." });
        }

        await using var stream = file.OpenReadStream();
        var photoUrl = await _employeeService.UploadPhotoAsync(
            id,
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            CurrentUserName ?? "system",
            _environment.ContentRootPath,
            cancellationToken);

        return Ok(new { message = "Photo uploaded successfully.", photoUrl });
    }

    [HttpGet("export/csv")]
    [Authorize(Policy = Permissions.ReportsRead)]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken)
    {
        // Delegate to ReportsController's shared CSV export via service
        var rows = await _employeeService.GetEmployeesAsync(new EmployeeListRequest
        {
            Page = 1,
            PageSize = int.MaxValue,
            SortBy = "createdDate",
            SortDirection = "asc"
        }, cancellationToken);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("EmployeeCode,FirstName,LastName,Email,PhoneNumber,Department,Status,DateOfJoining");
        foreach (var row in rows.Items)
        {
            sb.AppendLine(string.Join(',',
                CsvHelper.Escape(row.EmployeeCode),
                CsvHelper.Escape(row.FirstName),
                CsvHelper.Escape(row.LastName),
                CsvHelper.Escape(row.Email),
                CsvHelper.Escape(row.PhoneNumber),
                CsvHelper.Escape(row.Department),
                CsvHelper.Escape(row.Status),
                row.DateOfJoining.ToString("yyyy-MM-dd")));
        }

        return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv",
            $"employees_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
