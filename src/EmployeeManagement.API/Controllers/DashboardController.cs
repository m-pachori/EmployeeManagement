using Asp.Versioning;
using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Domain.Entities;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _memoryCache;

    public DashboardController(IUnitOfWork unitOfWork, IMemoryCache memoryCache)
    {
        _unitOfWork = unitOfWork;
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

            var employeeCount = await _unitOfWork.Repository<Employee>().Query().CountAsync(cancellationToken);
            var departmentCount = await _unitOfWork.Repository<Department>().Query().CountAsync(cancellationToken);
            var activeUserCount = await _unitOfWork.Repository<User>().Query().CountAsync(x => x.IsActive, cancellationToken);

            var lastLogins = await _unitOfWork.Repository<User>().Query()
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

            var recentActivity = await _unitOfWork.Repository<AuditLog>().Query()
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