using System.Linq.Expressions;
using EmployeeManagement.Domain.Common;

namespace EmployeeManagement.Application.Common.Interfaces;

/// <summary>
/// Generic repository abstraction used by all modules to access persisted entities.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    IQueryable<T> Query();

    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);
}
