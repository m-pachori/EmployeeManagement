using EmployeeManagement.Domain.Common;

namespace EmployeeManagement.Domain.Entities;

public class AuditLog : BaseEntity
{
    public int? UserId { get; set; }

    public User? User { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? Details { get; set; }

    public string? IpAddress { get; set; }
}