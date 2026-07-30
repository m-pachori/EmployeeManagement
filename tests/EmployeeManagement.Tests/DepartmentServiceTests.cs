using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Application.Departments.Dtos;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;
using EmployeeManagement.Infrastructure.Persistence;
using EmployeeManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Tests;

public class DepartmentServiceTests
{
    // -------------------------------------------------------------------------
    // GetAllAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WhenNoDepartments_ReturnsEmptyList()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var result = await sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDepartmentsOrderedByName()
    {
        await using var ctx = CreateDbContext();
        await SeedDepartmentAsync(ctx, "Zebra", "ZBR");
        await SeedDepartmentAsync(ctx, "Alpha", "ALP");
        var sut = CreateService(ctx);

        var result = await sut.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Zebra", result[1].Name);
    }

    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectDto()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx, "Engineering", "ENG");
        var sut = CreateService(ctx);

        var result = await sut.GetByIdAsync(dept.Id);

        Assert.Equal(dept.Id, result.Id);
        Assert.Equal("Engineering", result.Name);
        Assert.Equal("ENG", result.Code);
        Assert.True(result.IsActive);
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
    // CreateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_PersistsEntityAndReturnsId()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var id = await sut.CreateAsync(new UpsertDepartmentRequest
        {
            Name = "Finance",
            Code = "FIN",
            IsActive = true
        }, "tester");

        Assert.True(id > 0);
        var saved = await ctx.Departments.SingleAsync(x => x.Id == id);
        Assert.Equal("Finance", saved.Name);
        Assert.Equal("FIN", saved.Code);
        Assert.Equal("tester", saved.CreatedBy);
    }

    [Fact]
    public async Task CreateAsync_WhenBlankName_Throws400()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new UpsertDepartmentRequest { Name = " ", Code = "X" }, "tester"));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenBlankCode_Throws400()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new UpsertDepartmentRequest { Name = "HR", Code = "" }, "tester"));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateName_Throws409()
    {
        await using var ctx = CreateDbContext();
        await SeedDepartmentAsync(ctx, "Marketing", "MKT");
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new UpsertDepartmentRequest { Name = "Marketing", Code = "MKT2" }, "tester"));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateCode_Throws409()
    {
        await using var ctx = CreateDbContext();
        await SeedDepartmentAsync(ctx, "Marketing", "MKT");
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new UpsertDepartmentRequest { Name = "Marketing2", Code = "MKT" }, "tester"));

        Assert.Equal(409, ex.StatusCode);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_MutatesEntity()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx, "OldName", "OLD");
        var sut = CreateService(ctx);

        await sut.UpdateAsync(dept.Id, new UpsertDepartmentRequest
        {
            Name = "NewName",
            Code = "NEW",
            IsActive = false
        }, "editor");

        var updated = await ctx.Departments.SingleAsync(x => x.Id == dept.Id);
        Assert.Equal("NewName", updated.Name);
        Assert.Equal("NEW", updated.Code);
        Assert.False(updated.IsActive);
        Assert.Equal("editor", updated.UpdatedBy);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_Throws404()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UpdateAsync(999, new UpsertDepartmentRequest { Name = "X", Code = "X" }, "editor"));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_WhenDuplicateNameOnDifferentRow_Throws409()
    {
        await using var ctx = CreateDbContext();
        await SeedDepartmentAsync(ctx, "Existing", "EXS");
        var target = await SeedDepartmentAsync(ctx, "Target", "TGT");
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UpdateAsync(target.Id, new UpsertDepartmentRequest { Name = "Existing", Code = "TGT" }, "editor"));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_AllowsSameNameOnSameRow()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx, "Sales", "SAL");
        var sut = CreateService(ctx);

        // Should NOT throw — renaming to own current name
        await sut.UpdateAsync(dept.Id, new UpsertDepartmentRequest { Name = "Sales", Code = "SAL" }, "editor");

        var updated = await ctx.Departments.SingleAsync(x => x.Id == dept.Id);
        Assert.Equal("Sales", updated.Name);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_RemovesDepartment()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx, "Temp", "TMP");
        var sut = CreateService(ctx);

        await sut.DeleteAsync(dept.Id, "admin");

        Assert.False(await ctx.Departments.AnyAsync(x => x.Id == dept.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_Throws404()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.DeleteAsync(999, "admin"));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_WhenEmployeesAssigned_Throws409()
    {
        await using var ctx = CreateDbContext();
        var dept = await SeedDepartmentAsync(ctx, "Busy", "BSY");
        await SeedEmployeeAsync(ctx, dept.Id);
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.DeleteAsync(dept.Id, "admin"));

        Assert.Equal(409, ex.StatusCode);
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

    private static DepartmentService CreateService(ApplicationDbContext ctx)
    {
        var uow = new UnitOfWork(ctx);
        return new DepartmentService(uow, new AuditLogService(uow));
    }

    private static async Task<Department> SeedDepartmentAsync(
        ApplicationDbContext ctx, string name, string code)
    {
        var dept = new Department { Name = name, Code = code, IsActive = true };
        ctx.Departments.Add(dept);
        await ctx.SaveChangesAsync();
        return dept;
    }

    private static async Task SeedEmployeeAsync(ApplicationDbContext ctx, int departmentId)
    {
        ctx.Employees.Add(new Employee
        {
            EmployeeCode = $"E{Guid.NewGuid():N}"[..8],
            FirstName = "Test",
            LastName = "User",
            Email = $"{Guid.NewGuid():N}@test.com",
            DepartmentId = departmentId,
            DateOfJoining = DateTime.UtcNow.AddYears(-1),
            Status = EmployeeStatus.Active
        });
        await ctx.SaveChangesAsync();
    }
}
