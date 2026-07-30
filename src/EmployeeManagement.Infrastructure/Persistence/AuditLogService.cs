using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Persistence;

/// <summary>
/// Default <see cref="IAuditLogService"/> implementation backed by the shared IUnitOfWork.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditLogService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task RecordAsync(
        string eventType,
        string entityName,
        string? entityId,
        string details,
        int? userId,
        string? actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        return _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserId = userId,
            EventType = eventType,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            CreatedBy = actorName,
            UpdatedBy = actorName,
            IpAddress = ipAddress
        }, cancellationToken);
    }
}
