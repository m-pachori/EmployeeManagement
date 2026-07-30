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
[Route("api/v{version:apiVersion}/audit")]
[Authorize]
public class AuditController : ApiControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("logs")]
    [Authorize(Policy = Permissions.AuditRead)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? eventType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _unitOfWork.Repository<AuditLog>().Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(x => x.EventType == eventType);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.CreatedDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.CreatedDate <= to.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.EventType,
                x.EntityName,
                x.EntityId,
                x.Details,
                x.IpAddress,
                x.CreatedBy,
                x.CreatedDate
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            items
        });
    }
}