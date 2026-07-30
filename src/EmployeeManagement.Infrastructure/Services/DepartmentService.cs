using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Departments.Dtos;
using EmployeeManagement.Application.Departments.Interfaces;
using EmployeeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Infrastructure.Services;

/// <summary>
/// Application-layer service for Department business operations.
/// </summary>
public class DepartmentService : IDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public DepartmentService(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Repository<Department>().Query()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new DepartmentDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Description = x.Description,
                IsActive = x.IsActive,
                EmployeeCount = x.Employees.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Repository<Department>().Query()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new DepartmentDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Description = x.Description,
                IsActive = x.IsActive,
                EmployeeCount = x.Employees.Count
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(404, "Department not found.");
    }

    public async Task<int> CreateAsync(UpsertDepartmentRequest request, string actorName, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, existingDepartmentId: null, cancellationToken);

        var department = new Department
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            CreatedBy = actorName,
            UpdatedBy = actorName
        };

        await _unitOfWork.Repository<Department>().AddAsync(department, cancellationToken);
        await _auditLogService.RecordAsync("DepartmentCreate", nameof(Department), null,
            $"Created department '{department.Name}'.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return department.Id;
    }

    public async Task UpdateAsync(int id, UpsertDepartmentRequest request, string actorName, CancellationToken cancellationToken = default)
    {
        var department = await _unitOfWork.Repository<Department>().Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Department not found.");

        await ValidateAsync(request, existingDepartmentId: id, cancellationToken);

        department.Name = request.Name.Trim();
        department.Code = request.Code.Trim();
        department.Description = request.Description?.Trim();
        department.IsActive = request.IsActive;
        department.UpdatedBy = actorName;
        department.UpdatedDate = DateTime.UtcNow;

        await _auditLogService.RecordAsync("DepartmentUpdate", nameof(Department), id.ToString(),
            $"Updated department '{department.Name}'.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, string actorName, CancellationToken cancellationToken = default)
    {
        var department = await _unitOfWork.Repository<Department>().Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Department not found.");

        var hasEmployees = await _unitOfWork.Repository<Employee>().Query()
            .AnyAsync(x => x.DepartmentId == id, cancellationToken);
        if (hasEmployees)
            throw new ApiException(409, "Department cannot be deleted because employees are assigned to it.");

        _unitOfWork.Repository<Department>().Remove(department);
        await _auditLogService.RecordAsync("DepartmentDelete", nameof(Department), id.ToString(),
            $"Deleted department '{department.Name}'.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateAsync(UpsertDepartmentRequest request, int? existingDepartmentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
            throw new ApiException(400, "Department name and code are required.");

        var duplicateName = await _unitOfWork.Repository<Department>().Query()
            .AnyAsync(x => x.Name == request.Name && x.Id != existingDepartmentId, cancellationToken);
        if (duplicateName)
            throw new ApiException(409, "Department name already exists.");

        var duplicateCode = await _unitOfWork.Repository<Department>().Query()
            .AnyAsync(x => x.Code == request.Code && x.Id != existingDepartmentId, cancellationToken);
        if (duplicateCode)
            throw new ApiException(409, "Department code already exists.");
    }
}
