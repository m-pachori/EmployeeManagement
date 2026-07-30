using System.Security.Claims;
using EmployeeManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.API.Controllers;

/// <summary>
/// Shared base for authenticated API controllers. Centralizes helpers that were
/// previously copy-pasted with minor variations into every controller
/// (GetCurrentUserId, current actor name, client IP, and audit-log recording).
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// The current user's id from the "sub"/NameIdentifier claim, or null if absent/unparseable.
    /// </summary>
    protected int? GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out var userId) ? userId : null;
    }

    /// <summary>
    /// Same as <see cref="GetCurrentUserId"/> but throws if the claim is missing - for
    /// endpoints where an authenticated user id is mandatory (e.g. change-password).
    /// </summary>
    protected int RequireCurrentUserId() =>
        GetCurrentUserId() ?? throw new UnauthorizedAccessException("User identifier claim is missing.");

    protected string? CurrentUserName => User.Identity?.Name;

    protected string? ClientIpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>
    /// Records an audit log entry as part of the current unit of work, using the current
    /// request's user id/actor name/IP address. The caller is still responsible for calling
    /// IUnitOfWork.SaveChangesAsync so the entry commits atomically with the business change.
    /// </summary>
    protected Task RecordAuditLogAsync(
        IAuditLogService auditLogService,
        string eventType,
        string entityName,
        string? entityId,
        string details,
        CancellationToken cancellationToken = default)
    {
        return auditLogService.RecordAsync(
            eventType,
            entityName,
            entityId,
            details,
            GetCurrentUserId(),
            CurrentUserName,
            ClientIpAddress,
            cancellationToken);
    }
}
