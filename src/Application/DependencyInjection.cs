using FluentValidation;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Behaviours;
using Microsoft.Extensions.DependencyInjection;

namespace KartAdminService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            // Registration order is pipeline order (outermost first) - Logging wraps
            // Validation so every request's completion/duration is observed uniformly.
            configuration.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AdminActionExecutor>();

        return services;
    }
}
