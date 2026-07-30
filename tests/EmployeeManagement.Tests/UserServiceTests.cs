using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Application.Users.Dtos;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Infrastructure.Authentication;
using EmployeeManagement.Infrastructure.Persistence;
using EmployeeManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmployeeManagement.Tests;

public class UserServiceTests
{
    private const string ValidPassword = "ValidPass1@";

    // -------------------------------------------------------------------------
    // GetAllAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WhenNoUsers_ReturnsEmptyList()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var result = await sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsUsersWithRoleNames()
    {
        await using var ctx = CreateDbContext();
        var role = await SeedRoleAsync(ctx, "Admin");
        var user = await SeedUserAsync(ctx, "alice", "alice@test.com");
        ctx.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await ctx.SaveChangesAsync();
        var sut = CreateService(ctx);

        var result = await sut.GetAllAsync();

        var dto = Assert.Single(result);
        Assert.Equal("alice", dto.UserName);
        Assert.Contains("Admin", dto.Roles);
    }

    // -------------------------------------------------------------------------
    // CreateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_PersistsUserAndReturnsId()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var id = await sut.CreateAsync(new CreateUserRequest
        {
            UserName = "bob",
            Email = "bob@test.com",
            FirstName = "Bob",
            LastName = "Smith",
            Password = ValidPassword,
            IsActive = true
        }, "admin");

        Assert.True(id > 0);
        var saved = await ctx.Users.SingleAsync(x => x.Id == id);
        Assert.Equal("bob", saved.UserName);
        Assert.Equal("admin", saved.CreatedBy);
        Assert.NotEmpty(saved.PasswordHash);
    }

    [Fact]
    public async Task CreateAsync_AssignsRolesWhenProvided()
    {
        await using var ctx = CreateDbContext();
        var role = await SeedRoleAsync(ctx, "HR");
        var sut = CreateService(ctx);

        var id = await sut.CreateAsync(new CreateUserRequest
        {
            UserName = "carol",
            Email = "carol@test.com",
            FirstName = "Carol",
            LastName = "Jones",
            Password = ValidPassword,
            RoleIds = [role.Id]
        }, "admin");

        var mapping = await ctx.UserRoles.SingleAsync(x => x.UserId == id);
        Assert.Equal(role.Id, mapping.RoleId);
    }

    [Fact]
    public async Task CreateAsync_UsesConfiguredPasswordExpiryDays()
    {
        await using var ctx = CreateDbContext();
        var customOptions = new AuthOptions { PasswordExpiryDays = 30, MaxFailedAccessAttempts = 5 };
        var sut = CreateService(ctx, customOptions);

        var id = await sut.CreateAsync(new CreateUserRequest
        {
            UserName = "dave",
            Email = "dave@test.com",
            FirstName = "Dave",
            LastName = "Doe",
            Password = ValidPassword
        }, "admin");

        var saved = await ctx.Users.SingleAsync(x => x.Id == id);
        // expiry should be ~30 days from now (within a 5-second tolerance)
        var expectedExpiry = DateTime.UtcNow.AddDays(30);
        Assert.True(Math.Abs((saved.PasswordExpiresAtUtc - expectedExpiry).TotalSeconds) < 5);
    }

    [Theory]
    [InlineData("", "user@test.com", "First", "Last", ValidPassword)]   // blank username
    [InlineData("user", "", "First", "Last", ValidPassword)]            // blank email
    [InlineData("user", "user@test.com", "", "Last", ValidPassword)]    // blank first name
    [InlineData("user", "user@test.com", "First", "", ValidPassword)]   // blank last name
    [InlineData("user", "user@test.com", "First", "Last", "")]          // blank password
    public async Task CreateAsync_WhenRequiredFieldBlank_Throws400(
        string userName, string email, string firstName, string lastName, string password)
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new CreateUserRequest
            {
                UserName = userName,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Password = password
            }, "admin"));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenWeakPassword_Throws400()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new CreateUserRequest
            {
                UserName = "weakuser",
                Email = "weak@test.com",
                FirstName = "Weak",
                LastName = "User",
                Password = "tooshort"
            }, "admin"));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateUsername_Throws409()
    {
        await using var ctx = CreateDbContext();
        await SeedUserAsync(ctx, "alice", "alice@test.com");
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new CreateUserRequest
            {
                UserName = "alice",
                Email = "other@test.com",
                FirstName = "A",
                LastName = "B",
                Password = ValidPassword
            }, "admin"));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateEmail_Throws409()
    {
        await using var ctx = CreateDbContext();
        await SeedUserAsync(ctx, "alice", "alice@test.com");
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new CreateUserRequest
            {
                UserName = "alicenew",
                Email = "alice@test.com",
                FirstName = "A",
                LastName = "B",
                Password = ValidPassword
            }, "admin"));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenInvalidRoleId_Throws400()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.CreateAsync(new CreateUserRequest
            {
                UserName = "frank",
                Email = "frank@test.com",
                FirstName = "Frank",
                LastName = "F",
                Password = ValidPassword,
                RoleIds = [9999]
            }, "admin"));

        Assert.Equal(400, ex.StatusCode);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_MutatesEmailAndNames()
    {
        await using var ctx = CreateDbContext();
        var user = await SeedUserAsync(ctx, "grace", "grace@test.com");
        var sut = CreateService(ctx);

        await sut.UpdateAsync(user.Id, new UpdateUserRequest
        {
            Email = "grace.new@test.com",
            FirstName = "Grace",
            LastName = "Updated"
        }, "admin");

        var updated = await ctx.Users.SingleAsync(x => x.Id == user.Id);
        Assert.Equal("grace.new@test.com", updated.Email);
        Assert.Equal("Updated", updated.LastName);
        Assert.Equal("admin", updated.UpdatedBy);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_Throws404()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UpdateAsync(999, new UpdateUserRequest
            {
                Email = "x@test.com",
                FirstName = "X",
                LastName = "Y"
            }, "admin"));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_WhenDuplicateEmailOnDifferentUser_Throws409()
    {
        await using var ctx = CreateDbContext();
        await SeedUserAsync(ctx, "henry", "henry@test.com");
        var target = await SeedUserAsync(ctx, "ivan", "ivan@test.com");
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UpdateAsync(target.Id, new UpdateUserRequest
            {
                Email = "henry@test.com",
                FirstName = "Ivan",
                LastName = "I"
            }, "admin"));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_AllowsSameEmailOnSameUser()
    {
        await using var ctx = CreateDbContext();
        var user = await SeedUserAsync(ctx, "jane", "jane@test.com");
        var sut = CreateService(ctx);

        // keeping own email — must not throw
        await sut.UpdateAsync(user.Id, new UpdateUserRequest
        {
            Email = "jane@test.com",
            FirstName = "Jane",
            LastName = "Updated"
        }, "admin");

        var updated = await ctx.Users.SingleAsync(x => x.Id == user.Id);
        Assert.Equal("Updated", updated.LastName);
    }

    // -------------------------------------------------------------------------
    // AssignRolesAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AssignRolesAsync_ReplacesExistingRoleMappings()
    {
        await using var ctx = CreateDbContext();
        var user = await SeedUserAsync(ctx, "kim", "kim@test.com");
        var roleA = await SeedRoleAsync(ctx, "RoleA");
        var roleB = await SeedRoleAsync(ctx, "RoleB");
        ctx.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleA.Id });
        await ctx.SaveChangesAsync();
        var sut = CreateService(ctx);

        await sut.AssignRolesAsync(user.Id,
            new AssignRolesRequest { RoleIds = [roleB.Id] }, "admin");

        var mappings = await ctx.UserRoles.Where(x => x.UserId == user.Id).ToListAsync();
        Assert.Single(mappings);
        Assert.Equal(roleB.Id, mappings[0].RoleId);
    }

    [Fact]
    public async Task AssignRolesAsync_WhenUserNotFound_Throws404()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.AssignRolesAsync(999, new AssignRolesRequest { RoleIds = [] }, "admin"));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task AssignRolesAsync_WhenInvalidRoleId_Throws400()
    {
        await using var ctx = CreateDbContext();
        var user = await SeedUserAsync(ctx, "leo", "leo@test.com");
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.AssignRolesAsync(user.Id,
                new AssignRolesRequest { RoleIds = [9999] }, "admin"));

        Assert.Equal(400, ex.StatusCode);
    }

    // -------------------------------------------------------------------------
    // UpdateStatusAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateStatusAsync_DeactivatesUser()
    {
        await using var ctx = CreateDbContext();
        var user = await SeedUserAsync(ctx, "mike", "mike@test.com");
        var sut = CreateService(ctx);

        await sut.UpdateStatusAsync(user.Id, new UpdateUserStatusRequest { IsActive = false }, "admin");

        var updated = await ctx.Users.SingleAsync(x => x.Id == user.Id);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdateStatusAsync_ReactivatesUser()
    {
        await using var ctx = CreateDbContext();
        var user = await SeedUserAsync(ctx, "nina", "nina@test.com", isActive: false);
        var sut = CreateService(ctx);

        await sut.UpdateStatusAsync(user.Id, new UpdateUserStatusRequest { IsActive = true }, "admin");

        var updated = await ctx.Users.SingleAsync(x => x.Id == user.Id);
        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenNotFound_Throws404()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.UpdateStatusAsync(999, new UpdateUserStatusRequest { IsActive = false }, "admin"));

        Assert.Equal(404, ex.StatusCode);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_RemovesUser()
    {
        await using var ctx = CreateDbContext();
        var user = await SeedUserAsync(ctx, "oscar", "oscar@test.com");
        var sut = CreateService(ctx);

        await sut.DeleteAsync(user.Id, currentUserId: null, "admin");

        Assert.False(await ctx.Users.AnyAsync(x => x.Id == user.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_Throws404()
    {
        await using var ctx = CreateDbContext();
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.DeleteAsync(999, currentUserId: null, "admin"));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_WhenDeletingOwnAccount_Throws400()
    {
        await using var ctx = CreateDbContext();
        var user = await SeedUserAsync(ctx, "pat", "pat@test.com");
        var sut = CreateService(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            sut.DeleteAsync(user.Id, currentUserId: user.Id, "pat"));

        Assert.Equal(400, ex.StatusCode);
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

    private static UserService CreateService(ApplicationDbContext ctx, AuthOptions? authOptions = null)
    {
        var uow = new UnitOfWork(ctx);
        return new UserService(
            uow,
            new PasswordHasher<User>(),
            new AuditLogService(uow),
            Options.Create(authOptions ?? new AuthOptions
            {
                PasswordExpiryDays = 90,
                MaxFailedAccessAttempts = 5,
                LockoutMinutes = 15,
                ResetTokenMinutes = 30
            }));
    }

    private static async Task<User> SeedUserAsync(
        ApplicationDbContext ctx, string userName, string email, bool isActive = true)
    {
        var user = new User
        {
            UserName = userName,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "seeded-hash",
            IsActive = isActive,
            PasswordChangedAtUtc = DateTime.UtcNow,
            PasswordExpiresAtUtc = DateTime.UtcNow.AddDays(90)
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user;
    }

    private static async Task<Role> SeedRoleAsync(ApplicationDbContext ctx, string name)
    {
        var role = new Role { Name = name, Description = $"{name} desc" };
        ctx.Roles.Add(role);
        await ctx.SaveChangesAsync();
        return role;
    }
}
