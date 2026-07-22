using System.Linq.Expressions;

namespace EmployeeManagement.Application.Common.Interfaces;

/// <summary>
/// Generic repository abstraction used by all modules to access persisted entities.
/// Constrained to reference types (rather than BaseEntity) so that pure join entities
/// with composite keys (e.g. UserRole, RolePermission) can also be accessed through it.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    IQueryable<T> Query();

    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);

    void RemoveRange(IEnumerable<T> entities);
}
