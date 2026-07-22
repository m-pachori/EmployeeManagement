using System.Text;
using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
[Authorize(Policy = Permissions.ReportsRead)]
public class ReportsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("employees")]
    public async Task<IActionResult> EmployeeReport([FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        var rows = await _unitOfWork.Repository<Employee>().Query()
            .AsNoTracking()
            .Include(x => x.Department)
            .OrderBy(x => x.EmployeeCode)
            .Select(x => new[]
            {
                x.EmployeeCode,
                x.FirstName,
                x.LastName,
                x.Email,
                x.Department.Name,
                x.Status.ToString(),
                x.DateOfJoining.ToString("yyyy-MM-dd")
            })
            .ToListAsync(cancellationToken);

        return BuildExport(
            "employee_report",
            ["EmployeeCode", "FirstName", "LastName", "Email", "Department", "Status", "DateOfJoining"],
            rows,
            format);
    }

    [HttpGet("departments")]
    public async Task<IActionResult> DepartmentReport([FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        var rows = await _unitOfWork.Repository<Department>().Query()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new[]
            {
                x.Name,
                x.Code,
                x.IsActive ? "Active" : "Inactive",
                x.Employees.Count.ToString()
            })
            .ToListAsync(cancellationToken);

        return BuildExport(
            "department_report",
            ["Name", "Code", "Status", "EmployeeCount"],
            rows,
            format);
    }

    [HttpGet("users")]
    public async Task<IActionResult> UserReport([FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        var rows = await _unitOfWork.Repository<User>().Query()
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .OrderBy(x => x.UserName)
            .Select(x => new[]
            {
                x.UserName,
                x.Email,
                x.IsActive ? "Active" : "Inactive",
                string.Join(";", x.UserRoles.Select(ur => ur.Role.Name)),
                x.LastLoginAtUtc.HasValue ? x.LastLoginAtUtc.Value.ToString("u") : string.Empty
            })
            .ToListAsync(cancellationToken);

        return BuildExport(
            "user_report",
            ["UserName", "Email", "Status", "Roles", "LastLoginAtUtc"],
            rows,
            format);
    }

    [HttpGet("login-activity")]
    public async Task<IActionResult> LoginActivityReport([FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        var rows = await _unitOfWork.Repository<AuditLog>().Query()
            .AsNoTracking()
            .Where(x => x.EventType == "Login" || x.EventType == "Logout")
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => new[]
            {
                x.EventType,
                x.UserId.HasValue ? x.UserId.Value.ToString() : string.Empty,
                x.CreatedBy ?? string.Empty,
                x.IpAddress ?? string.Empty,
                x.CreatedDate.ToString("u")
            })
            .ToListAsync(cancellationToken);

        return BuildExport(
            "login_activity_report",
            ["EventType", "UserId", "UserName", "IpAddress", "CreatedAtUtc"],
            rows,
            format);
    }

    private IActionResult BuildExport(string fileNamePrefix, IReadOnlyList<string> header, IReadOnlyList<string[]> rows, string format)
    {
        format = string.IsNullOrWhiteSpace(format) ? "csv" : format.Trim().ToLowerInvariant();

        return format switch
        {
            "csv" => File(
                Encoding.UTF8.GetBytes(ToCsv(header, rows)),
                "text/csv",
                $"{fileNamePrefix}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv"),
            "excel" => File(
                Encoding.UTF8.GetBytes(ToTsv(header, rows)),
                "application/vnd.ms-excel",
                $"{fileNamePrefix}_{DateTime.UtcNow:yyyyMMddHHmmss}.xls"),
            "pdf" => File(
                BuildMinimalPdf(header, rows),
                "application/pdf",
                $"{fileNamePrefix}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf"),
            _ => BadRequest(new { message = "Supported formats are csv, excel, and pdf." })
        };
    }

    private static string ToCsv(IReadOnlyList<string> header, IReadOnlyList<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', header.Select(EscapeCsv)));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',', row.Select(EscapeCsv)));
        }

        return sb.ToString();
    }

    private static string ToTsv(IReadOnlyList<string> header, IReadOnlyList<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join('\t', header));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join('\t', row.Select(v => v.Replace('\t', ' '))));
        }

        return sb.ToString();
    }

    private static byte[] BuildMinimalPdf(IReadOnlyList<string> header, IReadOnlyList<string[]> rows)
    {
        var lines = new List<string>
        {
            string.Join(" | ", header)
        };

        lines.AddRange(rows.Select(r => string.Join(" | ", r)));
        var content = string.Join("\n", lines.Take(50));

        var stream = $"BT /F1 10 Tf 50 780 Td ({EscapePdf(content)}) Tj ET";
        var objects = new List<string>
        {
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj",
            "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj",
            "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj",
            $"5 0 obj << /Length {stream.Length} >> stream\n{stream}\nendstream endobj"
        };

        var sb = new StringBuilder();
        sb.AppendLine("%PDF-1.4");

        var offsets = new List<int>();
        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
            sb.AppendLine(obj);
        }

        var xrefPosition = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.AppendLine("xref");
        sb.AppendLine($"0 {objects.Count + 1}");
        sb.AppendLine("0000000000 65535 f ");

        foreach (var offset in offsets)
        {
            sb.AppendLine($"{offset:0000000000} 00000 n ");
        }

        sb.AppendLine("trailer");
        sb.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        sb.AppendLine("startxref");
        sb.AppendLine(xrefPosition.ToString());
        sb.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static string EscapePdf(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}