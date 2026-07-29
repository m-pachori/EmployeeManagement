using EmployeeManagement.Application.Authentication.Dtos;
using EmployeeManagement.Application.Common.Exceptions;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Infrastructure.Authentication;
using EmployeeManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EmployeeManagement.Tests;

public class AuthenticationTests
{
    [Theory]
    [InlineData("weak", false)]
    [InlineData("NoNumber@", false)]
    [InlineData("NoSpecial1", false)]
    [InlineData("lowercase1@", false)]
    [InlineData("UPPERCASE1@", false)]
    [InlineData("StrongPass1@", true)]
    public void PasswordPolicyValidator_ValidatesExpectedRules(string password, bool expected)
    {
        var isValid = PasswordPolicyValidator.IsValid(password);

        Assert.Equal(expected, isValid);
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokens_AndUpdatesLastLogin()
    {
        await using var context = CreateDbContext();
        var passwordHasher = new PasswordHasher<User>();
        var user = await SeedUserGraphAsync(context, passwordHasher, expiresAtUtc: DateTime.UtcNow.AddDays(5));

        var sut = CreateAuthService(context, passwordHasher);

        var result = await sut.LoginAsync(new LoginRequest
        {
            UserNameOrEmail = user.UserName,
            Password = "ValidPass1@"
        }, "127.0.0.1");

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.Contains("Admin", result.Roles);
        Assert.Contains("Users.Read", result.Permissions);

        var refreshedUser = await context.Users.SingleAsync(x => x.Id == user.Id);
        Assert.NotNull(refreshedUser.LastLoginAtUtc);
    }

    [Fact]
    public async Task LoginAsync_LocksAccount_AfterConfiguredFailedAttempts()
    {
        await using var context = CreateDbContext();
        var passwordHasher = new PasswordHasher<User>();
        var user = await SeedUserGraphAsync(context, passwordHasher, expiresAtUtc: DateTime.UtcNow.AddDays(5));

        var sut = CreateAuthService(
            context,
            passwordHasher,
            new AuthOptions { MaxFailedAccessAttempts = 1, LockoutMinutes = 10, PasswordExpiryDays = 90, ResetTokenMinutes = 30 });

        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.LoginAsync(new LoginRequest
        {
            UserNameOrEmail = user.UserName,
            Password = "WrongPassword1@"
        }, "127.0.0.1"));

        Assert.Equal(401, ex.StatusCode);

        var updatedUser = await context.Users.SingleAsync(x => x.Id == user.Id);
        Assert.NotNull(updatedUser.LockoutEndUtc);
    }

    [Fact]
    public async Task LoginAsync_RejectsExpiredPassword()
    {
        await using var context = CreateDbContext();
        var passwordHasher = new PasswordHasher<User>();
        var user = await SeedUserGraphAsync(context, passwordHasher, expiresAtUtc: DateTime.UtcNow.AddDays(-1));

        var sut = CreateAuthService(context, passwordHasher);

        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.LoginAsync(new LoginRequest
        {
            UserNameOrEmail = user.UserName,
            Password = "ValidPass1@"
        }, "127.0.0.1"));

        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task RefreshTokenAsync_RotatesRefreshToken()
    {
        await using var context = CreateDbContext();
        var passwordHasher = new PasswordHasher<User>();
        var user = await SeedUserGraphAsync(context, passwordHasher, expiresAtUtc: DateTime.UtcNow.AddDays(5));

        var sut = CreateAuthService(context, passwordHasher);

        var login = await sut.LoginAsync(new LoginRequest
        {
            UserNameOrEmail = user.UserName,
            Password = "ValidPass1@"
        }, "127.0.0.1");

        var refresh = await sut.RefreshTokenAsync(new RefreshTokenRequest
        {
            RefreshToken = login.RefreshToken
        }, "127.0.0.2");

        Assert.NotEqual(login.RefreshToken, refresh.RefreshToken);

        var tokens = await context.RefreshTokens
            .Where(x => x.UserId == user.Id)
            .ToListAsync();

        Assert.Equal(2, tokens.Count);
        Assert.Single(tokens.Where(x => x.RevokedAtUtc.HasValue));
        Assert.Single(tokens.Where(x => !x.RevokedAtUtc.HasValue));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static AuthService CreateAuthService(ApplicationDbContext context, PasswordHasher<User> passwordHasher, AuthOptions? authOptions = null)
    {
        return new AuthService(
            new UnitOfWork(context),
            passwordHasher,
            Options.Create(new JwtOptions
            {
                Issuer = "test",
                Audience = "test",
                SecretKey = "THIS_IS_A_TEST_SECRET_KEY_WITH_MINIMUM_LENGTH_12345",
                AccessTokenMinutes = 15,
                RefreshTokenDays = 7
            }),
            Options.Create(authOptions ?? new AuthOptions
            {
                MaxFailedAccessAttempts = 5,
                LockoutMinutes = 15,
                PasswordExpiryDays = 90,
                ResetTokenMinutes = 30
            }),
            new FakeHostEnvironment(),
            NullLogger<AuthService>.Instance);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "EmployeeManagement.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private static async Task<User> SeedUserGraphAsync(ApplicationDbContext context, PasswordHasher<User> passwordHasher, DateTime expiresAtUtc)
    {
        var permission = new Permission
        {
            Code = "Users.Read",
            Module = "Users",
            Action = "Read",
            Description = "Read users"
        };

        var role = new Role
        {
            Name = "Admin",
            Description = "Admin role",
            RolePermissions = [new RolePermission { Permission = permission }]
        };

        var user = new User
        {
            UserName = "admin",
            Email = "admin@test.local",
            FirstName = "System",
            LastName = "Admin",
            IsActive = true,
            PasswordExpiresAtUtc = expiresAtUtc,
            PasswordChangedAtUtc = DateTime.UtcNow,
            UserRoles = [new UserRole { Role = role }]
        };

        user.PasswordHash = passwordHasher.HashPassword(user, "ValidPass1@");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }
}