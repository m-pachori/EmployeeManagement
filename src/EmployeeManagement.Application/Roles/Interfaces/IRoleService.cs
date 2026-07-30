using EmployeeManagement.Application.Roles.Dtos;

namespace EmployeeManagement.Application.Roles.Interfaces;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<int> CreateAsync(UpsertRoleRequest request, string actorName, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, UpsertRoleRequest request, string actorName, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, string actorName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    Task AssignPermissionsAsync(int id, AssignPermissionsRequest request, string actorName, CancellationToken cancellationToken = default);
}
