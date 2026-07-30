using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Application;

/// <summary>
/// Registers Application-layer service interfaces. Concrete implementations live in
/// Infrastructure and are registered by AddInfrastructure(). This method is the
/// extension point for any pure-Application-layer registrations (e.g. pipeline
/// behaviours, validators) that do not depend on Infrastructure.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
