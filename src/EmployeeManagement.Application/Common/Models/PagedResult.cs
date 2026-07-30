namespace EmployeeManagement.Application.Common.Models;

/// <summary>
/// Generic paged result envelope returned by paginated list queries.
/// </summary>
public class PagedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
}
