namespace EmployeeManagement.API;

/// <summary>
/// Shared CSV escaping utility — eliminates the duplicate EscapeCsv method
/// that previously existed in both EmployeesController and ReportsController (TD-11).
/// </summary>
internal static class CsvHelper
{
    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
