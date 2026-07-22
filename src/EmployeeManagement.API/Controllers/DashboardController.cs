using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _memoryCache;

    public DashboardController(ApplicationDbContext context, IMemoryCache memoryCache)
    {
        _context = context;
        _memoryCache = memoryCache;
    }

    [HttpGet("summary")]
    [Authorize(Policy = Permissions.DashboardRead)]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _memoryCache.GetOrCreateAsync("dashboard:summary", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);

            var employeeCount = await _context.Employees.CountAsync(cancellationToken);
            var departmentCount = await _context.Departments.CountAsync(cancellationToken);
            var activeUserCount = await _context.Users.CountAsync(x => x.IsActive, cancellationToken);

            var lastLogins = await _context.Users
                .AsNoTracking()
                .Where(x => x.LastLoginAtUtc.HasValue)
                .OrderByDescending(x => x.LastLoginAtUtc)
                .Take(5)
                .Select(x => new
                {
                    x.UserName,
                    x.Email,
                    x.LastLoginAtUtc
                })
                .ToListAsync(cancellationToken);

            var recentActivity = await _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedDate)
                .Take(10)
                .Select(x => new
                {
                    x.EventType,
                    x.EntityName,
                    x.EntityId,
                    x.CreatedDate,
                    x.CreatedBy
                })
                .ToListAsync(cancellationToken);

            return new
            {
                employeeCount,
                departmentCount,
                activeUserCount,
                lastLogins,
                recentActivity
            };
        });

        return Ok(result);
    }
}