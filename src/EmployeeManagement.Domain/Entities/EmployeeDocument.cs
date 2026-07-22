using EmployeeManagement.Domain.Common;

namespace EmployeeManagement.Domain.Entities;

public class EmployeeDocument : BaseEntity
{
    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}