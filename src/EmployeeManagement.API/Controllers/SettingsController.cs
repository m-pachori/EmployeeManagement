using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Infrastructure.Persistence;
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
    private readonly ApplicationDbContext _context;

    public SettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.SettingsRead)]
    public async Task<IActionResult> GetSettings([FromQuery] string? category, CancellationToken cancellationToken)
    {
        var query = _context.SystemSettings.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        var settings = await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Key)
            .Select(x => new
            {
                x.Id,
                x.Category,
                x.Key,
                x.Value,
                x.Description,
                x.UpdatedDate
            })
            .ToListAsync(cancellationToken);

        return Ok(settings);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.SettingsWrite)]
    public async Task<IActionResult> UpsertSetting([FromBody] UpsertSettingRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Category) || string.IsNullOrWhiteSpace(request.Key))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Category and key are required.");
        }

        var setting = await _context.SystemSettings
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

            _context.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = request.Value ?? string.Empty;
            setting.Description = request.Description;
            setting.UpdatedBy = User.Identity?.Name;
            setting.UpdatedDate = DateTime.UtcNow;
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = GetCurrentUserId(),
            EventType = "SettingUpsert",
            EntityName = nameof(SystemSetting),
            EntityId = setting.Id == 0 ? null : setting.Id.ToString(),
            Details = $"Upserted setting '{request.Category}.{request.Key}'.",
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _context.SaveChangesAsync(cancellationToken);
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