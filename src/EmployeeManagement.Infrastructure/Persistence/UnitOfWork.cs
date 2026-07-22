using System.Collections.Concurrent;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Domain.Common;

namespace EmployeeManagement.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of the unit of work pattern, providing shared repository instances
/// and a single SaveChanges commit point per request/transaction.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRepository<T> Repository<T>() where T : BaseEntity
    {
        return (IRepository<T>)_repositories.GetOrAdd(typeof(T), _ => new Repository<T>(_context));
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
