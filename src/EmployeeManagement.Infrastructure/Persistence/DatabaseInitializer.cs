using EmployeeManagement.Application.Common.Constants;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmployeeManagement.Infrastructure.Persistence;

public class DatabaseInitializer
{
    private const string DefaultAdminPassword = "Admin@123";

    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly AuthOptions _authOptions;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher,
        IOptions<AuthOptions> authOptions,
        IConfiguration configuration,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _authOptions = authOptions.Value;
        _configuration = configuration;
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
        if (await _unitOfWork.Repository<Permission>().Query().AnyAsync(cancellationToken))
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

        await _unitOfWork.Repository<Permission>().AddRangeAsync(permissions, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        if (await _unitOfWork.Repository<Role>().Query().AnyAsync(cancellationToken))
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

        await _unitOfWork.Repository<Role>().AddRangeAsync(roles, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var adminRole = roles.Single(x => x.Name == DefaultRoles.Admin);
        var allPermissions = await _unitOfWork.Repository<Permission>().GetAllAsync(cancellationToken);

        var rolePermissions = allPermissions.Select(permission => new RolePermission
        {
            RoleId = adminRole.Id,
            PermissionId = permission.Id
        });

        await _unitOfWork.Repository<RolePermission>().AddRangeAsync(rolePermissions, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        if (await _unitOfWork.Repository<User>().Query().AnyAsync(x => x.UserName == "admin", cancellationToken))
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

        admin.PasswordHash = _passwordHasher.HashPassword(admin, GetAdminSeedPassword());

        await _unitOfWork.Repository<User>().AddAsync(admin, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var adminRole = await _unitOfWork.Repository<Role>().Query().SingleAsync(x => x.Name == DefaultRoles.Admin, cancellationToken);
        await _unitOfWork.Repository<UserRole>().AddAsync(new UserRole { UserId = admin.Id, RoleId = adminRole.Id }, cancellationToken);

        await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserId = admin.Id,
            EventType = "SeedAdmin",
            EntityName = nameof(User),
            EntityId = admin.Id.ToString(),
            Details = "Default admin user seeded.",
            CreatedBy = "system",
            UpdatedBy = "system"
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Default admin user seeded with username '{Username}'.", admin.UserName);
    }

    private string GetAdminSeedPassword()
    {
        var configuredPassword = _configuration["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(configuredPassword))
        {
            return configuredPassword;
        }

        _logger.LogWarning(
            "No 'Seed:AdminPassword' configuration value was supplied; falling back to the default seed password. " +
            "Set the Seed__AdminPassword environment variable (or Seed:AdminPassword configuration key) to a strong " +
            "value before deploying to any shared or production environment, and change the admin password after first login.");

        return DefaultAdminPassword;
    }
}