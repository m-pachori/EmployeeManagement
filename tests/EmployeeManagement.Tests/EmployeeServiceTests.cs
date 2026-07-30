using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Application.Employees.Dtos;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;
using EmployeeManagement.Infrastructure.Persistence;
using EmployeeManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Tests;

public class EmployeeServiceTests
{
    // -------------------------------------------------------------------------
    // GetEmployeesAsync — list / filter / pagination / sort
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetEmployeesAsync_WhenNoEmployees_ReturnsEmptyPage()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        var sut = CreateService(ctx);

        var result = await sut.GetEmployeesAsync(new EmployeeListRequest { Page = 1, PageSize = 10 });

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetEmployeesAsync_ReturnsAllEmployees()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        await SeedEmployeeAsync(ctx, dept.Id, code: "E001", firstName: "Alice");
        await SeedEmployeeAsync(ctx, dept.Id, code: "E002", firstName: "Bob");
        var sut = CreateService(ctx);

        var result = await sut.GetEmployeesAsync(new EmployeeListRequest { Page = 1, PageSize = 10 });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetEmployeesAsync_FiltersByDepartmentId()
    {
        await using var ctx = CreateDbContext();
        var deptA = await SeedDepartmentAsync(ctx, name: "DeptA", code: "DA");
        var deptB = await SeedDepartmentAsync(ctx, name: "DeptB", code: "DB");
        await SeedEmployeeAsync(ctx, deptA.Id, code: "A1", firstName: "Alice");
        await SeedEmployeeAsync(ctx, deptB.Id, code: "B1", firstName: "Bob");
        var sut = CreateService(ctx);

        var result = await sut.GetEmployeesAsync(new EmployeeListRequest
        {
            DepartmentId = deptA.Id,
            Page = 1, PageSize = 10
        });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Alice", result.Items[0].FirstName);
    }

    [Fact]
    public async Task GetEmployeesAsync_FiltersByStatus()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        await SeedEmployeeAsync(ctx, dept.Id, code: "ACT1", status: EmployeeStatus.Active);
        await SeedEmployeeAsync(ctx, dept.Id, code: "INA1", status: EmployeeStatus.Inactive);
        var sut = CreateService(ctx);

        var result = await sut.GetEmployeesAsync(new EmployeeListRequest
        {
            Status = EmployeeStatus.Inactive,
            Page = 1, PageSize = 10
        });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Inactive", result.Items[0].Status);
    }

    [Fact]
    public async Task GetEmployeesAsync_PaginatesCorrectly()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        for (var i = 1; i <= 5; i++)
        {
            await SeedEmployeeAsync(ctx, dept.Id, code: $"P{i:000}");
        }
        var sut = CreateService(ctx);

        var result = await sut.GetEmployeesAsync(new EmployeeListRequest
        {
            Page = 2,
            PageSize = 2
        });

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetEmployeesAsync_SortsByLastNameAscending()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        await SeedEmployeeAsync(ctx, dept.Id, code: "S1", lastName: "Zebra");
        await SeedEmployeeAsync(ctx, dept.Id, code: "S2", lastName: "Alpha");
        var sut = CreateService(ctx);

        var result = await sut.GetEmployeesAsync(new EmployeeListRequest
        {
            SortBy = "lastname",
            SortDirection = "asc",
            Page = 1, PageSize = 10
        });

        Assert.Equal("Alpha", result.Items[0].LastName);
        Assert.Equal("Zebra", result.Items[1].LastName);
    }

    // NOTE: EF.Functions.Like is not translated by the In-Memory provider.
    // The in-memory provider falls back to evaluating the expression client-side using
    // string pattern matching. We verify the filter narrows results correctly using
    // seeds where only one employee's name contains the keyword.
    [Fact]
    public async Task GetEmployeesAsync_SearchFiltersResults()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        await SeedEmployeeAsync(ctx, dept.Id, code: "X01", firstName: "Unique");
        await SeedEmployeeAsync(ctx, dept.Id, code: "X02", firstName: "Common");
        var sut = CreateService(ctx);

        var result = await sut.GetEmployeesAsync(new EmployeeListRequest
        {
            Search = "Unique",
            Page = 1, PageSize = 10
        });

        // In-memory provider evaluates EF.Functions.Like client-side; 1 match expected
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Unique", result.Items[0].FirstName);
    }

    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectDto()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        var emp = await SeedEmployeeAsync(ctx, dept.Id, code: "G001", firstName: "George");
        var sut = CreateService(ctx);

        var result = await sut.GetByIdAsync(emp.Id);

        Assert.Equal(emp.Id, result.Id);
        Assert.Equal("George", result.FirstName);
        Assert.Equal("G001", result.EmployeeCode);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_Throws404()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.GetByIdAsync(999));

        Assert.Equal(404, ex.StatusCode);
    }

    // -------------------------------------------------------------------------
    // CreateAsync — happy path + validation guards
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_PersistsEmployeeAndReturnsId()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        var sut = CreateService(ctx);

        var id = await sut.CreateAsync(new CreateEmployeeRequest
        {
            EmployeeCode = "NEW01",
            FirstName = "New",
            LastName = "Employee",
            Email = "new@test.com",
            DateOfJoining = DateTime.UtcNow.AddYears(-1),
            DepartmentId = dept.Id,
            Status = EmployeeStatus.Active
        }, "admin");

        Assert.True(id > 0);
        var saved = await ctx.Employees.SingleAsync(x => x.Id == id);
        Assert.Equal("NEW01", saved.EmployeeCode);
        Assert.Equal("admin", saved.CreatedBy);
    }

    [Fact]
    public async Task CreateAsync_WhenBlankRequiredFields_Throws400()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new CreateEmployeeRequest
            {
                EmployeeCode = "",
                FirstName = "X",
                LastName = "Y",
                Email = "x@test.com",
                DateOfJoining = DateTime.UtcNow.AddDays(-1),
                DepartmentId = dept.Id
            }, "admin"));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenFutureDateOfJoining_Throws400()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new CreateEmployeeRequest
            {
                EmployeeCode = "FUT01",
                FirstName = "Future",
                LastName = "Hire",
                Email = "future@test.com",
                DateOfJoining = DateTime.UtcNow.AddDays(10),
                DepartmentId = dept.Id
            }, "admin"));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenNegativeSalary_Throws400()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new CreateEmployeeRequest
            {
                EmployeeCode = "SAL01",
                FirstName = "A",
                LastName = "B",
                Email = "sal@test.com",
                DateOfJoining = DateTime.UtcNow.AddYears(-1),
                DepartmentId = dept.Id,
                Salary = -100m
            }, "admin"));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenInactiveDepartment_Throws400()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx, isActive: false);
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new CreateEmployeeRequest
            {
                EmployeeCode = "DEPT01",
                FirstName = "A",
                LastName = "B",
                Email = "dept@test.com",
                DateOfJoining = DateTime.UtcNow.AddYears(-1),
                DepartmentId = dept.Id
            }, "admin"));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenManagerDoesNotExist_Throws400()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new CreateEmployeeRequest
            {
                EmployeeCode = "MGR01",
                FirstName = "A",
                LastName = "B",
                Email = "mgr@test.com",
                DateOfJoining = DateTime.UtcNow.AddYears(-1),
                DepartmentId = dept.Id,
                ManagerId = 9999
            }, "admin"));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateEmployeeCode_Throws409()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        await SeedEmployeeAsync(ctx, dept.Id, code: "DUP01");
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new CreateEmployeeRequest
            {
                EmployeeCode = "DUP01",
                FirstName = "A",
                LastName = "B",
                Email = "unique@test.com",
                DateOfJoining = DateTime.UtcNow.AddYears(-1),
                DepartmentId = dept.Id
            }, "admin"));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateEmail_Throws409()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        await SeedEmployeeAsync(ctx, dept.Id, code: "EML01", email: "taken@test.com");
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new CreateEmployeeRequest
            {
                EmployeeCode = "EML02",
                FirstName = "A",
                LastName = "B",
                Email = "taken@test.com",
                DateOfJoining = DateTime.UtcNow.AddYears(-1),
                DepartmentId = dept.Id
            }, "admin"));

        Assert.Equal(409, ex.StatusCode);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_MutatesEmployee()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        var emp = await SeedEmployeeAsync(ctx, dept.Id, code: "UPD01", firstName: "Old");
        var sut = CreateService(ctx);

        await sut.UpdateAsync(emp.Id, new UpdateEmployeeRequest
        {
            EmployeeCode = "UPD01",
            FirstName = "Updated",
            LastName = emp.LastName,
            Email = emp.Email,
            DateOfJoining = emp.DateOfJoining,
            DepartmentId = dept.Id,
            Status = EmployeeStatus.Active
        }, "editor");

        var saved = await ctx.Employees.SingleAsync(x => x.Id == emp.Id);
        Assert.Equal("Updated", saved.FirstName);
        Assert.Equal("editor", saved.UpdatedBy);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_Throws404()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UpdateAsync(999, new UpdateEmployeeRequest
            {
                EmployeeCode = "X",
                FirstName = "X",
                LastName = "X",
                Email = "x@test.com",
                DateOfJoining = DateTime.UtcNow.AddYears(-1),
                DepartmentId = dept.Id
            }, "editor"));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_WhenSelfManager_Throws400()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        var emp = await SeedEmployeeAsync(ctx, dept.Id, code: "SELF1");
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UpdateAsync(emp.Id, new UpdateEmployeeRequest
            {
                EmployeeCode = emp.EmployeeCode,
                FirstName = emp.FirstName,
                LastName = emp.LastName,
                Email = emp.Email,
                DateOfJoining = emp.DateOfJoining,
                DepartmentId = dept.Id,
                ManagerId = emp.Id   // self-reference
            }, "editor"));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_AllowsSameCodeOnSameRow()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        var emp = await SeedEmployeeAsync(ctx, dept.Id, code: "SAME1");
        var sut = CreateService(ctx);

        // Using own current code must not throw conflict
        await sut.UpdateAsync(emp.Id, new UpdateEmployeeRequest
        {
            EmployeeCode = "SAME1",
            FirstName = "Updated",
            LastName = emp.LastName,
            Email = emp.Email,
            DateOfJoining = emp.DateOfJoining,
            DepartmentId = dept.Id
        }, "editor");

        var saved = await ctx.Employees.SingleAsync(x => x.Id == emp.Id);
        Assert.Equal("Updated", saved.FirstName);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_RemovesEmployee()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        var emp = await SeedEmployeeAsync(ctx, dept.Id, code: "DEL01");
        var sut = CreateService(ctx);

        await sut.DeleteAsync(emp.Id, "admin");

        Assert.False(await ctx.Employees.AnyAsync(x => x.Id == emp.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_Throws404()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.DeleteAsync(999, "admin"));

        Assert.Equal(404, ex.StatusCode);
    }

    // -------------------------------------------------------------------------
    // UploadPhotoAsync — validation guards + happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UploadPhotoAsync_WhenZeroSize_Throws400()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UploadPhotoAsync(1, Stream.Null, "photo.jpg", "image/jpeg",
                sizeInBytes: 0, "admin", Path.GetTempPath()));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UploadPhotoAsync_WhenOversized_Throws400()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UploadPhotoAsync(1, Stream.Null, "photo.jpg", "image/jpeg",
                sizeInBytes: 300 * 1024, "admin", Path.GetTempPath()));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UploadPhotoAsync_WhenWrongExtension_Throws400()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UploadPhotoAsync(1, Stream.Null, "photo.png", "image/jpeg",
                sizeInBytes: 1024, "admin", Path.GetTempPath()));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UploadPhotoAsync_WhenWrongMimeType_Throws400()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UploadPhotoAsync(1, Stream.Null, "photo.jpg", "image/png",
                sizeInBytes: 1024, "admin", Path.GetTempPath()));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UploadPhotoAsync_WhenInvalidMagicBytes_Throws400()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        var emp = await SeedEmployeeAsync(ctx, dept.Id, code: "PHO01");
        var sut = CreateService(ctx);

        // Stream with WRONG magic bytes (PNG header: 89 50 4E 47...)
        using var fakeStream = MakePhotoStream(validMagicBytes: false);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UploadPhotoAsync(emp.Id, fakeStream, "photo.jpg", "image/jpeg",
                sizeInBytes: fakeStream.Length, "admin", Path.GetTempPath()));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UploadPhotoAsync_WhenEmployeeNotFound_Throws404()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        using var stream = MakePhotoStream(validMagicBytes: true);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UploadPhotoAsync(9999, stream, "photo.jpg", "image/jpeg",
                sizeInBytes: stream.Length, "admin", Path.GetTempPath()));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task UploadPhotoAsync_HappyPath_PersistsPhotoUrlAndReturnsIt()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx);
        var emp = await SeedEmployeeAsync(ctx, dept.Id, code: "PHO02");
        var sut = CreateService(ctx);
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ems_test_{Guid.NewGuid():N}");

        using var stream = MakePhotoStream(validMagicBytes: true);
        try
        {
            var photoUrl = await sut.UploadPhotoAsync(
                emp.Id, stream, "photo.jpg", "image/jpeg",
                sizeInBytes: stream.Length, "admin", tempRoot);

            Assert.StartsWith($"/uploads/employees/{emp.Id}/", photoUrl);

            var saved = await ctx.Employees.SingleAsync(x => x.Id == emp.Id);
            Assert.Equal(photoUrl, saved.PhotoUrl);
        }
        finally
        {
            // clean up temp file
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static EmployeeService CreateService(ApplicationDbContext ctx)
    {
        var uow = new UnitOfWork(ctx);
        return new EmployeeService(uow, new AuditLogService(uow));
    }

    private static async Task<Department> SeedDepartmentAsync(
        ApplicationDbContext ctx,
        string name = "Engineering",
        string code = "ENG",
        bool isActive = true)
    {
        var dept = new Department { Name = name, Code = code, IsActive = isActive };
        ctx.Departments.Add(dept);
        await ctx.SaveChangesAsync();
        return dept;
    }

    private static async Task<Employee> SeedEmployeeAsync(
        ApplicationDbContext ctx,
        int departmentId,
        string code = "E001",
        string firstName = "Test",
        string lastName = "Employee",
        string? email = null,
        EmployeeStatus status = EmployeeStatus.Active)
    {
        var employee = new Employee
        {
            EmployeeCode = code,
            FirstName = firstName,
            LastName = lastName,
            Email = email ?? $"{code.ToLower()}@test.com",
            DepartmentId = departmentId,
            DateOfJoining = DateTime.UtcNow.AddYears(-1),
            Status = status
        };
        ctx.Employees.Add(employee);
        await ctx.SaveChangesAsync();
        return employee;
    }

    /// <summary>
    /// Builds a minimal in-memory stream with correct or incorrect JPEG magic bytes.
    /// Valid JPEG starts with FF D8 FF; this helper lets tests control that.
    /// </summary>
    private static MemoryStream MakePhotoStream(bool validMagicBytes)
    {
        var bytes = new byte[16];
        if (validMagicBytes)
        {
            bytes[0] = 0xFF;
            bytes[1] = 0xD8;
            bytes[2] = 0xFF;
        }
        else
        {
            // PNG magic: 89 50 4E 47
            bytes[0] = 0x89;
            bytes[1] = 0x50;
            bytes[2] = 0x4E;
            bytes[3] = 0x47;
        }
        return new MemoryStream(bytes);
    }
}
