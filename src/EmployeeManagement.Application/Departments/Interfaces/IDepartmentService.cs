using EmployeeManagement.Application.Departments.Dtos;

namespace EmployeeManagement.Application.Departments.Interfaces;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DepartmentDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(UpsertDepartmentRequest request, string actorName, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, UpsertDepartmentRequest request, string actorName, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, string actorName, CancellationToken cancellationToken = default);
}
