using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Application;

/// <summary>
/// Registers Application-layer services with the dependency injection container.
/// Business/use-case services (Employees, Departments, Users, Roles, Settings, Reports, etc.)
/// will be added here as each module is implemented.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
