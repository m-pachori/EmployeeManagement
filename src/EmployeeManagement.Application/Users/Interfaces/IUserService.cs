using EmployeeManagement.Application.Users.Dtos;

namespace EmployeeManagement.Application.Users.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CreateUserRequest request, string actorName, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, UpdateUserRequest request, string actorName, CancellationToken cancellationToken = default);

    Task AssignRolesAsync(int id, AssignRolesRequest request, string actorName, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(int id, UpdateUserStatusRequest request, string actorName, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, int? currentUserId, string actorName, CancellationToken cancellationToken = default);
}
