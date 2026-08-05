using Kart.Shared.Messaging;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Infrastructure.ExternalClients;
using KartAdminService.Infrastructure.Messaging;
using KartAdminService.Infrastructure.Persistence;
using KartAdminService.Infrastructure.Persistence.Repositories;
using KartAdminService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace KartAdminService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AdminDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("AdminDatabase")));

        services.AddScoped<IPermissionGrantRepository, PermissionGrantRepository>();
        services.AddScoped<IAdminActionRepository, AdminActionRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentPrincipal, HttpCurrentPrincipal>();

        AddMessaging(services, configuration);
        AddDownstreamClients(services, configuration);

        return services;
    }

    /// <summary>
    /// contracts/message-bus-manifest.json is the single source of truth for this service's
    /// entire RabbitMQ topology (admin.exchange, no queues — Admin publishes
    /// AdminActionPerformed and consumes nothing). Nothing messaging-related is hardcoded in
    /// C#: the manifest is loaded once here and shared as a singleton;
    /// RabbitMqTopologyProvisioner scans it to declare the topology. IConnectionFactory only
    /// builds config, it does not connect eagerly, so registering it here is safe even if
    /// RabbitMQ is unreachable at startup — RabbitMqTopologyStartupHostedService and
    /// OutboxRelayHostedService each own their own retrying connection.
    /// </summary>
    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddKartMessageBusManifest(sp => sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value.ManifestPath);
        services.AddKartRabbitMqConnectionFactory(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
            return new RabbitMqConnectionSettings(options.HostName, Port: options.Port, UserName: options.UserName, Password: options.Password);
        });
        services.AddKartRabbitMqTopologyStartup();
        services.AddHostedService<OutboxRelayHostedService>();
    }

    /// <summary>
    /// One typed HttpClient per owning service (architecture.md Dependencies table), each with
    /// its own independent Polly policy instance — design-decisions.md's "Resilience Pattern for
    /// Outbound Calls to Owning Services": a short per-call timeout, a small bounded retry, and
    /// one circuit breaker per peer rather than one shared breaker.
    /// </summary>
    private static void AddDownstreamClients(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DownstreamServiceOptions>(configuration.GetSection(DownstreamServiceOptions.SectionName));
        services.Configure<IdentityClientCredentialsOptions>(configuration.GetSection(IdentityClientCredentialsOptions.SectionName));

        services.AddHttpClient<IProductServiceClient, ProductServiceClient>((sp, client) =>
            ConfigureEndpoint(client, sp.GetRequiredService<IOptions<DownstreamServiceOptions>>().Value.Product))
            .AddPolicyHandler((sp, _) => ResiliencePolicies.BuildPolicy(
                TimeSpan.FromMilliseconds(sp.GetRequiredService<IOptions<DownstreamServiceOptions>>().Value.Product.TimeoutMilliseconds)));

        services.AddHttpClient<ICategoryServiceClient, CategoryServiceClient>((sp, client) =>
            ConfigureEndpoint(client, sp.GetRequiredService<IOptions<DownstreamServiceOptions>>().Value.Category))
            .AddPolicyHandler((sp, _) => ResiliencePolicies.BuildPolicy(
                TimeSpan.FromMilliseconds(sp.GetRequiredService<IOptions<DownstreamServiceOptions>>().Value.Category.TimeoutMilliseconds)));

        services.AddHttpClient<IOfferServiceClient, OfferServiceClient>((sp, client) =>
            ConfigureEndpoint(client, sp.GetRequiredService<IOptions<DownstreamServiceOptions>>().Value.Offer))
            .AddPolicyHandler((sp, _) => ResiliencePolicies.BuildPolicy(
                TimeSpan.FromMilliseconds(sp.GetRequiredService<IOptions<DownstreamServiceOptions>>().Value.Offer.TimeoutMilliseconds)));

        services.AddHttpClient<IInventoryServiceClient, InventoryServiceClient>((sp, client) =>
            ConfigureEndpoint(client, sp.GetRequiredService<IOptions<DownstreamServiceOptions>>().Value.Inventory))
            .AddPolicyHandler((sp, _) => ResiliencePolicies.BuildPolicy(
                TimeSpan.FromMilliseconds(sp.GetRequiredService<IOptions<DownstreamServiceOptions>>().Value.Inventory.TimeoutMilliseconds)));

        // Identity's client-credentials token endpoint gets its own plain HttpClient (no
        // Idempotency-Key semantics apply to a token fetch) plus the shared circuit breaker for
        // the actual lock/unlock calls.
        services.AddHttpClient<IdentityClientCredentialsTokenProvider>((sp, client) =>
            ConfigureEndpoint(client, sp.GetRequiredService<IOptions<DownstreamServiceOptions>>().Value.Identity));

        services.AddHttpClient<IIdentityAdminClient, IdentityAdminClient>((sp, client) =>
            ConfigureEndpoint(client, sp.GetRequiredService<IOptions<DownstreamServiceOptions>>().Value.Identity))
            .AddPolicyHandler((sp, _) => ResiliencePolicies.BuildPolicy(
                TimeSpan.FromMilliseconds(sp.GetRequiredService<IOptions<DownstreamServiceOptions>>().Value.Identity.TimeoutMilliseconds)));
    }

    private static void ConfigureEndpoint(HttpClient client, DownstreamServiceOptions.ServiceEndpoint endpoint)
    {
        if (!string.IsNullOrWhiteSpace(endpoint.BaseUrl))
        {
            client.BaseAddress = new Uri(endpoint.BaseUrl);
        }

        // The overall per-attempt timeout is enforced by ResiliencePolicies' Polly timeout
        // policy (which can distinguish "timed out, safe to retry" from other faults);
        // HttpClient's own Timeout is set generously above that so it never fires first.
        client.Timeout = TimeSpan.FromSeconds(10);
    }
}
