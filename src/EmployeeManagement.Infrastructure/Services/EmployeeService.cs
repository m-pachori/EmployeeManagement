using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Common.Models;
using EmployeeManagement.Application.Employees.Dtos;
using EmployeeManagement.Application.Employees.Interfaces;
using EmployeeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Infrastructure.Services;

/// <summary>
/// Application-layer service for Employee business operations.
/// Controllers delegate here; this class owns validation, mapping, and persistence.
/// </summary>
public class EmployeeService : IEmployeeService
{
    private const long MaxPhotoSizeInBytes = 250 * 1024;
    private static readonly string[] AllowedPhotoExtensions = [".jpg", ".jpeg"];
    private static readonly string[] AllowedPhotoContentTypes = ["image/jpeg"];

    // JPEG magic bytes: every valid JPEG starts with FF D8 FF
    private static readonly byte[] JpegMagicBytes = [0xFF, 0xD8, 0xFF];

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public EmployeeService(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<EmployeeDto>> GetEmployeesAsync(EmployeeListRequest request, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<Employee>().Query()
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Manager)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // TD-05: Use EF.Functions.Like so SQL Server can leverage a case-insensitive
            // collation index instead of calling LOWER() which is non-sargable.
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.Like(x.FirstName, pattern)
                || EF.Functions.Like(x.LastName, pattern)
                || EF.Functions.Like(x.Email, pattern)
                || EF.Functions.Like(x.EmployeeCode, pattern));
        }

        if (request.DepartmentId.HasValue)
            query = query.Where(x => x.DepartmentId == request.DepartmentId.Value);

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);

        query = (request.SortBy.ToLowerInvariant(), request.SortDirection.ToLowerInvariant()) switch
        {
            ("firstname", "asc") => query.OrderBy(x => x.FirstName),
            ("firstname", _) => query.OrderByDescending(x => x.FirstName),
            ("lastname", "asc") => query.OrderBy(x => x.LastName),
            ("lastname", _) => query.OrderByDescending(x => x.LastName),
            ("email", "asc") => query.OrderBy(x => x.Email),
            ("email", _) => query.OrderByDescending(x => x.Email),
            ("dateofjoining", "asc") => query.OrderBy(x => x.DateOfJoining),
            ("dateofjoining", _) => query.OrderByDescending(x => x.DateOfJoining),
            ("createddate", "asc") => query.OrderBy(x => x.CreatedDate),
            _ => query.OrderByDescending(x => x.CreatedDate)
        };

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new EmployeeDto
            {
                Id = x.Id,
                EmployeeCode = x.EmployeeCode,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                PhotoUrl = x.PhotoUrl,
                Designation = x.Designation,
                Salary = x.Salary,
                DateOfJoining = x.DateOfJoining,
                Status = x.Status.ToString(),
                Department = x.Department.Name,
                DepartmentId = x.DepartmentId,
                ManagerId = x.ManagerId,
                ManagerName = x.Manager == null ? null : x.Manager.FirstName + " " + x.Manager.LastName,
                CreatedDate = x.CreatedDate
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<EmployeeDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _unitOfWork.Repository<Employee>().Query()
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Manager)
            .Where(x => x.Id == id)
            .Select(x => new EmployeeDto
            {
                Id = x.Id,
                EmployeeCode = x.EmployeeCode,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                PhotoUrl = x.PhotoUrl,
                Designation = x.Designation,
                Salary = x.Salary,
                DateOfJoining = x.DateOfJoining,
                Status = x.Status.ToString(),
                DepartmentId = x.DepartmentId,
                Department = x.Department.Name,
                ManagerId = x.ManagerId,
                ManagerName = x.Manager == null ? null : x.Manager.FirstName + " " + x.Manager.LastName,
                CreatedDate = x.CreatedDate
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(404, "Employee not found.");

        return employee;
    }

    public async Task<int> CreateAsync(CreateEmployeeRequest request, string actorName, CancellationToken cancellationToken = default)
    {
        await ValidateRequestAsync(request.EmployeeCode, request.FirstName, request.LastName,
            request.Email, request.PhoneNumber, request.DateOfJoining, request.Salary,
            request.DepartmentId, request.ManagerId, existingEmployeeId: null, cancellationToken);

        var employee = new Employee
        {
            EmployeeCode = request.EmployeeCode.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Designation = request.Designation?.Trim(),
            Salary = request.Salary,
            DateOfJoining = request.DateOfJoining,
            Status = request.Status,
            DepartmentId = request.DepartmentId,
            ManagerId = request.ManagerId,
            CreatedBy = actorName,
            UpdatedBy = actorName
        };

        await _unitOfWork.Repository<Employee>().AddAsync(employee, cancellationToken);
        await _auditLogService.RecordAsync("EmployeeCreate", nameof(Employee), null,
            $"Created employee '{employee.EmployeeCode}'.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return employee.Id;
    }

    public async Task UpdateAsync(int id, UpdateEmployeeRequest request, string actorName, CancellationToken cancellationToken = default)
    {
        var employee = await _unitOfWork.Repository<Employee>().Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Employee not found.");

        await ValidateRequestAsync(request.EmployeeCode, request.FirstName, request.LastName,
            request.Email, request.PhoneNumber, request.DateOfJoining, request.Salary,
            request.DepartmentId, request.ManagerId, existingEmployeeId: id, cancellationToken);

        employee.EmployeeCode = request.EmployeeCode.Trim();
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Email = request.Email.Trim();
        employee.PhoneNumber = request.PhoneNumber?.Trim();
        employee.Designation = request.Designation?.Trim();
        employee.Salary = request.Salary;
        employee.DateOfJoining = request.DateOfJoining;
        employee.Status = request.Status;
        employee.DepartmentId = request.DepartmentId;
        employee.ManagerId = request.ManagerId;
        employee.UpdatedBy = actorName;
        employee.UpdatedDate = DateTime.UtcNow;

        await _auditLogService.RecordAsync("EmployeeUpdate", nameof(Employee), id.ToString(),
            $"Updated employee '{employee.EmployeeCode}'.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, string actorName, CancellationToken cancellationToken = default)
    {
        var employee = await _unitOfWork.Repository<Employee>().Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Employee not found.");

        _unitOfWork.Repository<Employee>().Remove(employee);
        await _auditLogService.RecordAsync("EmployeeDelete", nameof(Employee), id.ToString(),
            $"Deleted employee '{employee.EmployeeCode}'.", null, actorName, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> UploadPhotoAsync(
        int id,
        Stream photoStream,
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string actorName,
        string contentRootPath,
        CancellationToken cancellationToken = default)
    {
        if (sizeInBytes == 0)
            throw new ApiException(400, "Photo file is required.");

        if (sizeInBytes > MaxPhotoSizeInBytes)
            throw new ApiException(400, "Photo file size must not exceed 250 KB.");

        var extension = Path.GetExtension(originalFileName)?.ToLowerInvariant() ?? string.Empty;
        var normalizedContentType = contentType.ToLowerInvariant();

        if (!AllowedPhotoExtensions.Contains(extension) || !AllowedPhotoContentTypes.Contains(normalizedContentType))
            throw new ApiException(400, "Only JPG photo files are allowed.");

        // TD-07: verify JPEG magic bytes (FF D8 FF) to prevent content-type spoofing
        var header = new byte[3];
        var bytesRead = await photoStream.ReadAsync(header.AsMemory(0, 3), cancellationToken);
        if (bytesRead < 3 || !header.SequenceEqual(JpegMagicBytes))
            throw new ApiException(400, "File content does not match a valid JPEG image.");

        photoStream.Seek(0, SeekOrigin.Begin);

        var employee = await _unitOfWork.Repository<Employee>().Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Employee not found.");

        var uploadsRoot = Path.Combine(contentRootPath, "uploads", "employees", id.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"photo_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        var absolutePath = Path.Combine(uploadsRoot, fileName);

        await using (var fileStream = File.Create(absolutePath))
        {
            await photoStream.CopyToAsync(fileStream, cancellationToken);
        }

        employee.PhotoUrl = $"/uploads/employees/{id}/{fileName}";
        employee.UpdatedBy = actorName;
        employee.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return employee.PhotoUrl;
    }

    private static readonly System.Text.RegularExpressions.Regex PhoneRegex =
        new(@"^\+?[0-9\s\-()]{7,20}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private async Task ValidateRequestAsync(
        string employeeCode,
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        DateTime dateOfJoining,
        decimal? salary,
        int departmentId,
        int? managerId,
        int? existingEmployeeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(employeeCode)
            || string.IsNullOrWhiteSpace(firstName)
            || string.IsNullOrWhiteSpace(lastName)
            || string.IsNullOrWhiteSpace(email))
        {
            throw new ApiException(400, "Employee code, first name, last name, and email are required.");
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber) && !PhoneRegex.IsMatch(phoneNumber))
            throw new ApiException(400, "Phone number must contain only digits, spaces, and the characters + - ( ) and be 7-20 characters long.");

        if (dateOfJoining.Date > DateTime.UtcNow.Date)
            throw new ApiException(400, "Date of joining cannot be in the future.");

        if (salary.HasValue && salary.Value <= 0)
            throw new ApiException(400, "Salary must be a positive value.");

        var departmentExists = await _unitOfWork.Repository<Department>().Query()
            .AnyAsync(x => x.Id == departmentId && x.IsActive, cancellationToken);
        if (!departmentExists)
            throw new ApiException(400, "Department does not exist or is inactive.");

        if (managerId.HasValue)
        {
            if (managerId.Value == existingEmployeeId)
                throw new ApiException(400, "An employee cannot be their own manager.");

            var managerExists = await _unitOfWork.Repository<Employee>().Query()
                .AnyAsync(x => x.Id == managerId.Value, cancellationToken);
            if (!managerExists)
                throw new ApiException(400, "Manager does not exist.");
        }

        var duplicateCode = await _unitOfWork.Repository<Employee>().Query()
            .AnyAsync(x => x.EmployeeCode == employeeCode && x.Id != existingEmployeeId, cancellationToken);
        if (duplicateCode)
            throw new ApiException(409, "Employee code already exists.");

        var duplicateEmail = await _unitOfWork.Repository<Employee>().Query()
            .AnyAsync(x => x.Email == email && x.Id != existingEmployeeId, cancellationToken);
        if (duplicateEmail)
            throw new ApiException(409, "Employee email already exists.");
    }
}
