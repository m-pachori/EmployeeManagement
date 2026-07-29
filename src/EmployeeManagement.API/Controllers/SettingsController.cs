using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public SettingsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.SettingsRead)]
    public async Task<IActionResult> GetSettings([FromQuery] string? category, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<SystemSetting>().Query().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        var settings = await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Key)
            .ToListAsync(cancellationToken);

        // SECURITY: never return secret-like values (e.g. SMTP password/API keys) in
        // cleartext (CWE-312). Values are still stored/updated normally; only the read
        // response is masked, so any caller with Settings.Read (not just Settings.Write)
        // can't harvest credentials.
        var response = settings.Select(x => new
        {
            x.Id,
            x.Category,
            x.Key,
            Value = IsSensitiveSetting(x.Key) ? MaskValue(x.Value) : x.Value,
            x.Description,
            x.UpdatedDate
        });

        return Ok(response);
    }

    private static readonly string[] SensitiveKeyMarkers =
    {
        "password", "secret", "apikey", "api_key", "token", "connectionstring", "privatekey"
    };

    private static bool IsSensitiveSetting(string key) =>
        SensitiveKeyMarkers.Any(marker => key.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static string MaskValue(string value) => string.IsNullOrEmpty(value) ? value : "••••••••";

    [HttpPost]
    [Authorize(Policy = Permissions.SettingsWrite)]
    public async Task<IActionResult> UpsertSetting([FromBody] UpsertSettingRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Category) || string.IsNullOrWhiteSpace(request.Key))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Category and key are required.");
        }

        var setting = await _unitOfWork.Repository<SystemSetting>().Query()
            .FirstOrDefaultAsync(x => x.Category == request.Category && x.Key == request.Key, cancellationToken);

        if (setting is null)
        {
            setting = new SystemSetting
            {
                Category = request.Category.Trim(),
                Key = request.Key.Trim(),
                Value = request.Value ?? string.Empty,
                Description = request.Description,
                CreatedBy = User.Identity?.Name,
                UpdatedBy = User.Identity?.Name
            };

            await _unitOfWork.Repository<SystemSetting>().AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.Value = request.Value ?? string.Empty;
            setting.Description = request.Description;
            setting.UpdatedBy = User.Identity?.Name;
            setting.UpdatedDate = DateTime.UtcNow;
        }

        await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserId = GetCurrentUserId(),
            EventType = "SettingUpsert",
            EntityName = nameof(SystemSetting),
            EntityId = setting.Id == 0 ? null : setting.Id.ToString(),
            Details = $"Upserted setting '{request.Category}.{request.Key}'.",
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Setting saved successfully." });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out var userId) ? userId : null;
    }
}

public class UpsertSettingRequest
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
}