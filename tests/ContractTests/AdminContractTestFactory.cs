using Kart.Shared.Messaging;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace KartAdminService.ContractTests;

/// <summary>
/// Boots the real Api + Application pipeline but swaps PostgreSQL and every downstream
/// owning-service HTTP client for in-memory/fake implementations, and real Identity-issued JWT
/// validation for a header-driven test scheme - these tests check the HTTP wire contract
/// (status codes, JSON field names, RBAC/Idempotency-Key gating) against api-contract.yaml, not
/// persistence/RLS/resilience mechanics (already covered by IntegrationTests).
/// </summary>
public sealed class AdminContractTestFactory : WebApplicationFactory<Program>
{
    public InMemoryPermissionGrantRepository GrantRepository { get; } = new();
    public InMemoryAdminActionRepository ActionRepository { get; } = new();
    public FakeProductServiceClient ProductClient { get; } = new();
    public FakeCategoryServiceClient CategoryClient { get; } = new();
    public FakeAttributeServiceClient AttributeClient { get; } = new();
    public FakeOfferServiceClient OfferClient { get; } = new();
    public FakeIdentityAdminClient IdentityClient { get; } = new();
    public FakeInventoryServiceClient InventoryClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Tells StartupConnectivityChecks to skip itself - this factory swaps every real
        // dependency below for in-memory fakes, so there's nothing for it to connect to.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPermissionGrantRepository>();
            services.AddSingleton<IPermissionGrantRepository>(GrantRepository);

            services.RemoveAll<IAdminActionRepository>();
            services.AddSingleton<IAdminActionRepository>(ActionRepository);

            services.RemoveAll<IUnitOfWork>();
            services.AddSingleton<IUnitOfWork, NoOpUnitOfWork>();

            services.RemoveAll<IProductServiceClient>();
            services.AddSingleton<IProductServiceClient>(ProductClient);

            services.RemoveAll<ICategoryServiceClient>();
            services.AddSingleton<ICategoryServiceClient>(CategoryClient);

            services.RemoveAll<IAttributeServiceClient>();
            services.AddSingleton<IAttributeServiceClient>(AttributeClient);

            services.RemoveAll<IOfferServiceClient>();
            services.AddSingleton<IOfferServiceClient>(OfferClient);

            services.RemoveAll<IIdentityAdminClient>();
            services.AddSingleton<IIdentityAdminClient>(IdentityClient);

            services.RemoveAll<IInventoryServiceClient>();
            services.AddSingleton<IInventoryServiceClient>(InventoryClient);

            // No real RabbitMQ/Postgres in the contract-test environment - these tests assert
            // HTTP shape, not event publication or persistence (covered separately).
            RemoveHostedService<RabbitMqTopologyStartupHostedService>(services);
            RemoveHostedService<OutboxRelayHostedService>(services);

            services.AddAuthentication(TestAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultScheme = TestAuthenticationHandler.SchemeName;
            });
        });
    }

    private static void RemoveHostedService<T>(IServiceCollection services)
        where T : class, IHostedService
    {
        var descriptor = services.FirstOrDefault(d => d.ImplementationType == typeof(T));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }
    }
}
