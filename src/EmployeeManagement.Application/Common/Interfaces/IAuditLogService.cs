namespace EmployeeManagement.Application.Common.Interfaces;

/// <summary>
/// Records audit trail entries for state-changing operations. Extracted as an
/// Application-layer service so controllers no longer hand-construct an AuditLog
/// entity at every write action (previously duplicated ~20+ times across controllers).
/// Implementations add the entry to the current unit of work without saving - callers
/// commit it together with their own business changes via IUnitOfWork.SaveChangesAsync.
/// </summary>
public interface IAuditLogService
{
    Task RecordAsync(
        string eventType,
        string entityName,
        string? entityId,
        string details,
        int? userId,
        string? actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
