using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmployeeManagement.Infrastructure.Persistence;

public class DatabaseInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly AuthOptions _authOptions;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        ApplicationDbContext context,
        IPasswordHasher<User> passwordHasher,
        IOptions<AuthOptions> authOptions,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _authOptions = authOptions.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);

        await SeedPermissionsAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedAdminUserAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        if (await _context.Permissions.AnyAsync(cancellationToken))
        {
            return;
        }

        var permissions = Permissions.All
            .Select(code =>
            {
                var parts = code.Split('.');
                var module = parts[0];
                var action = parts.Length > 1 ? parts[1] : "Read";

                return new Permission
                {
                    Code = code,
                    Module = module,
                    Action = action,
                    Description = $"Allows {action.ToLowerInvariant()} access to {module}.",
                    CreatedBy = "system",
                    UpdatedBy = "system"
                };
            })
            .ToList();

        _context.Permissions.AddRange(permissions);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        if (await _context.Roles.AnyAsync(cancellationToken))
        {
            return;
        }

        var roles = new[]
        {
            new Role { Name = DefaultRoles.Admin, Description = "System administrator", CreatedBy = "system", UpdatedBy = "system" },
            new Role { Name = DefaultRoles.Manager, Description = "Manager", CreatedBy = "system", UpdatedBy = "system" },
            new Role { Name = DefaultRoles.HR, Description = "HR user", CreatedBy = "system", UpdatedBy = "system" },
            new Role { Name = DefaultRoles.Employee, Description = "Standard employee", CreatedBy = "system", UpdatedBy = "system" }
        };

        _context.Roles.AddRange(roles);
        await _context.SaveChangesAsync(cancellationToken);

        var adminRole = roles.Single(x => x.Name == DefaultRoles.Admin);
        var allPermissions = await _context.Permissions.ToListAsync(cancellationToken);

        var rolePermissions = allPermissions.Select(permission => new RolePermission
        {
            RoleId = adminRole.Id,
            PermissionId = permission.Id
        });

        _context.RolePermissions.AddRange(rolePermissions);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(x => x.UserName == "admin", cancellationToken))
        {
            return;
        }

        var admin = new User
        {
            UserName = "admin",
            Email = "admin@local.dev",
            FirstName = "System",
            LastName = "Administrator",
            IsActive = true,
            PasswordChangedAtUtc = DateTime.UtcNow,
            PasswordExpiresAtUtc = DateTime.UtcNow.AddDays(_authOptions.PasswordExpiryDays),
            CreatedBy = "system",
            UpdatedBy = "system"
        };

        admin.PasswordHash = _passwordHasher.HashPassword(admin, "Admin@123");

        _context.Users.Add(admin);
        await _context.SaveChangesAsync(cancellationToken);

        var adminRole = await _context.Roles.SingleAsync(x => x.Name == DefaultRoles.Admin, cancellationToken);
        _context.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = admin.Id,
            EventType = "SeedAdmin",
            EntityName = nameof(User),
            EntityId = admin.Id.ToString(),
            Details = "Default admin user seeded.",
            CreatedBy = "system",
            UpdatedBy = "system"
        });

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Default admin user seeded with username '{Username}'.", admin.UserName);
    }
}