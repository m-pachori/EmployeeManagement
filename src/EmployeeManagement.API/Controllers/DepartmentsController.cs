using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Departments.Dtos;
using EmployeeManagement.Application.Departments.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/departments")]
[Authorize]
public class DepartmentsController : ApiControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.DepartmentsRead)]
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
    {
        var departments = await _departmentService.GetAllAsync(cancellationToken);
        return Ok(departments);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.DepartmentsRead)]
    public async Task<IActionResult> GetDepartmentById(int id, CancellationToken cancellationToken)
    {
        var department = await _departmentService.GetByIdAsync(id, cancellationToken);
        return Ok(department);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.DepartmentsWrite)]
    public async Task<IActionResult> CreateDepartment([FromBody] UpsertDepartmentRequest request, CancellationToken cancellationToken)
    {
        var id = await _departmentService.CreateAsync(request, CurrentUserName ?? "system", cancellationToken);
        return CreatedAtAction(nameof(GetDepartmentById), new { id, version = "1" }, id);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.DepartmentsWrite)]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpsertDepartmentRequest request, CancellationToken cancellationToken)
    {
        await _departmentService.UpdateAsync(id, request, CurrentUserName ?? "system", cancellationToken);
        return Ok(new { message = "Department updated successfully." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.DepartmentsWrite)]
    public async Task<IActionResult> DeleteDepartment(int id, CancellationToken cancellationToken)
    {
        await _departmentService.DeleteAsync(id, CurrentUserName ?? "system", cancellationToken);
        return Ok(new { message = "Department deleted successfully." });
    }
}
