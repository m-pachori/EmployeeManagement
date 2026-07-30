using EmployeeManagement.Application.Common.Models;
using EmployeeManagement.Application.Employees.Dtos;

namespace EmployeeManagement.Application.Employees.Interfaces;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeDto>> GetEmployeesAsync(EmployeeListRequest request, CancellationToken cancellationToken = default);

    Task<EmployeeDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CreateEmployeeRequest request, string actorName, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, UpdateEmployeeRequest request, string actorName, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, string actorName, CancellationToken cancellationToken = default);

    Task<string> UploadPhotoAsync(int id, Stream photoStream, string originalFileName, string contentType, long sizeInBytes, string actorName, string contentRootPath, CancellationToken cancellationToken = default);
}
