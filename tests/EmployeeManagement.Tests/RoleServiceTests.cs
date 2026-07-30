using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Application.Roles.Dtos;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Infrastructure.Persistence;
using EmployeeManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Tests;

public class RoleServiceTests
{
    // -------------------------------------------------------------------------
    // GetAllAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WhenNoRoles_ReturnsEmptyList()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var result = await sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCorrectCounts()
    {
        await using var ctx = CreateDbContext();
        var permission = await SeedPermissionAsync(ctx, "Employees.Read");
        var role = await SeedRoleAsync(ctx, "Viewer");
        ctx.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await ctx.SaveChangesAsync();
        var sut = CreateService(ctx);

        var result = await sut.GetAllAsync();

        var dto = Assert.Single(result);
        Assert.Equal("Viewer", dto.Name);
        Assert.Equal(1, dto.PermissionCount);
        Assert.Equal(0, dto.UserCount);
    }

    // -------------------------------------------------------------------------
    // CreateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_PersistsRoleAndReturnsId()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var id = await sut.CreateAsync(new UpsertRoleRequest { Name = "Manager", Description = "Manages" }, "admin");

        Assert.True(id > 0);
        var saved = await ctx.Roles.SingleAsync(x => x.Id == id);
        Assert.Equal("Manager", saved.Name);
        Assert.Equal("admin", saved.CreatedBy);
    }

    [Fact]
    public async Task CreateAsync_WhenBlankName_Throws400()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new UpsertRoleRequest { Name = "  " }, "admin"));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateName_Throws409()
    {
        await using var ctx = CreateDbContext();
        await SeedRoleAsync(ctx, "HR");
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new UpsertRoleRequest { Name = "HR" }, "admin"));

        Assert.Equal(409, ex.StatusCode);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_MutatesRole()
    {
        await using var ctx = CreateDbContext();
        var role = await SeedRoleAsync(ctx, "OldRole");
        var sut = CreateService(ctx);

        await sut.UpdateAsync(role.Id, new UpsertRoleRequest { Name = "NewRole", Description = "Updated" }, "editor");

        var updated = await ctx.Roles.SingleAsync(x => x.Id == role.Id);
        Assert.Equal("NewRole", updated.Name);
        Assert.Equal("Updated", updated.Description);
        Assert.Equal("editor", updated.UpdatedBy);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_Throws404()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UpdateAsync(999, new UpsertRoleRequest { Name = "X" }, "editor"));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_WhenDuplicateNameOnDifferentRow_Throws409()
    {
        await using var ctx = CreateDbContext();
        await SeedRoleAsync(ctx, "Existing");
        var target = await SeedRoleAsync(ctx, "Target");
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UpdateAsync(target.Id, new UpsertRoleRequest { Name = "Existing" }, "editor"));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_AllowsSameNameOnSameRow()
    {
        await using var ctx = CreateDbContext();
        var role = await SeedRoleAsync(ctx, "Admin");
        var sut = CreateService(ctx);

        // updating with own current name must not throw
        await sut.UpdateAsync(role.Id, new UpsertRoleRequest { Name = "Admin", Description = "Changed desc" }, "editor");

        var updated = await ctx.Roles.SingleAsync(x => x.Id == role.Id);
        Assert.Equal("Changed desc", updated.Description);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_RemovesRoleAndPermissionMappings()
    {
        await using var ctx = CreateDbContext();
        var role = await SeedRoleAsync(ctx, "TempRole");
        var perm = await SeedPermissionAsync(ctx, "Audit.Read");
        ctx.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
        await ctx.SaveChangesAsync();
        var sut = CreateService(ctx);

        await sut.DeleteAsync(role.Id, "admin");

        Assert.False(await ctx.Roles.AnyAsync(x => x.Id == role.Id));
        Assert.False(await ctx.RolePermissions.AnyAsync(x => x.RoleId == role.Id));
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
    public async Task DeleteAsync_WhenRoleHasUsers_Throws409()
    {
        await using var ctx = CreateDbContext();
        var role = await SeedRoleAsync(ctx, "InUse");
        await SeedUserWithRoleAsync(ctx, role.Id);
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.DeleteAsync(role.Id, "admin"));

        Assert.Equal(409, ex.StatusCode);
    }

    // -------------------------------------------------------------------------
    // GetPermissionsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPermissionsAsync_ReturnsPermissionsOrderedByModuleThenAction()
    {
        await using var ctx = CreateDbContext();
        await SeedPermissionAsync(ctx, "Users.Write", module: "Users", action: "Write");
        await SeedPermissionAsync(ctx, "Departments.Read", module: "Departments", action: "Read");
        await SeedPermissionAsync(ctx, "Users.Read", module: "Users", action: "Read");
        var sut = CreateService(ctx);

        var result = await sut.GetPermissionsAsync();

        Assert.Equal(3, result.Count);
        // Departments first, then Users.Read, then Users.Write
        Assert.Equal("Departments", result[0].Module);
        Assert.Equal("Users", result[1].Module);
        Assert.Equal("Read", result[1].Action);
        Assert.Equal("Write", result[2].Action);
    }

    // -------------------------------------------------------------------------
    // AssignPermissionsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AssignPermissionsAsync_ReplacesExistingMappings()
    {
        await using var ctx = CreateDbContext();
        var role = await SeedRoleAsync(ctx, "Editor");
        var permA = await SeedPermissionAsync(ctx, "Employees.Read");
        var permB = await SeedPermissionAsync(ctx, "Employees.Write");
        // start with permA assigned
        ctx.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permA.Id });
        await ctx.SaveChangesAsync();
        var sut = CreateService(ctx);

        // replace with permB only
        await sut.AssignPermissionsAsync(role.Id,
            new AssignPermissionsRequest { PermissionIds = [permB.Id] }, "admin");

        var mappings = await ctx.RolePermissions.Where(x => x.RoleId == role.Id).ToListAsync();
        Assert.Single(mappings);
        Assert.Equal(permB.Id, mappings[0].PermissionId);
    }

    [Fact]
    public async Task AssignPermissionsAsync_WhenRoleNotFound_Throws404()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.AssignPermissionsAsync(999, new AssignPermissionsRequest { PermissionIds = [] }, "admin"));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task AssignPermissionsAsync_WhenInvalidPermissionId_Throws400()
    {
        await using var ctx = CreateDbContext();
        var role = await SeedRoleAsync(ctx, "Broken");
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.AssignPermissionsAsync(role.Id,
                new AssignPermissionsRequest { PermissionIds = [9999] }, "admin"));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task AssignPermissionsAsync_WithEmptyList_ClearsAllMappings()
    {
        await using var ctx = CreateDbContext();
        var role = await SeedRoleAsync(ctx, "Cleared");
        var perm = await SeedPermissionAsync(ctx, "Settings.Read");
        ctx.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
        await ctx.SaveChangesAsync();
        var sut = CreateService(ctx);

        await sut.AssignPermissionsAsync(role.Id,
            new AssignPermissionsRequest { PermissionIds = [] }, "admin");

        Assert.False(await ctx.RolePermissions.AnyAsync(x => x.RoleId == role.Id));
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

    private static RoleService CreateService(ApplicationDbContext ctx)
    {
        var uow = new UnitOfWork(ctx);
        return new RoleService(uow, new AuditLogService(uow));
    }

    private static async Task<Role> SeedRoleAsync(ApplicationDbContext ctx, string name)
    {
        var role = new Role { Name = name, Description = $"{name} description" };
        ctx.Roles.Add(role);
        await ctx.SaveChangesAsync();
        return role;
    }

    private static async Task<Permission> SeedPermissionAsync(
        ApplicationDbContext ctx, string code,
        string? module = null, string? action = null)
    {
        var parts = code.Split('.');
        var perm = new Permission
        {
            Code = code,
            Module = module ?? (parts.Length > 0 ? parts[0] : "Module"),
            Action = action ?? (parts.Length > 1 ? parts[1] : "Action"),
            Description = $"{code} permission"
        };
        ctx.Permissions.Add(perm);
        await ctx.SaveChangesAsync();
        return perm;
    }

    private static async Task SeedUserWithRoleAsync(ApplicationDbContext ctx, int roleId)
    {
        var user = new User
        {
            UserName = $"user_{Guid.NewGuid():N}"[..16],
            Email = $"{Guid.NewGuid():N}@test.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            PasswordChangedAtUtc = DateTime.UtcNow,
            PasswordExpiresAtUtc = DateTime.UtcNow.AddDays(90)
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        ctx.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        await ctx.SaveChangesAsync();
    }
}
