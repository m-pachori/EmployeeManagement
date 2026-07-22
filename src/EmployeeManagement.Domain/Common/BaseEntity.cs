namespace EmployeeManagement.Domain.Common;

/// <summary>
/// Base class for all domain entities providing identity and audit metadata.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
