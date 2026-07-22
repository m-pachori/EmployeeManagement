using EmployeeManagement.Domain.Common;

namespace EmployeeManagement.Application.Common.Interfaces;

/// <summary>
/// Coordinates repository operations and commits changes within a single transaction boundary.
/// </summary>
public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : BaseEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
