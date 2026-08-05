namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>
/// Binds the "DownstreamServices" configuration section — base URLs for the five owning
/// services Admin proxies to (architecture.md Dependencies table). Product/Category/Offer/
/// Inventory don't have a deployed write API yet (api-contract.yaml's own header note); these
/// clients are written against the documented contract shape with a configurable base URL so
/// they work unchanged once each service ships its real endpoint. Identity's lock/unlock is
/// real and confirmed today (InternalUserEndpoints.cs).
/// </summary>
public sealed class DownstreamServiceOptions
{
    public const string SectionName = "DownstreamServices";

    public ServiceEndpoint Product { get; set; } = new();
    public ServiceEndpoint Category { get; set; } = new();
    public ServiceEndpoint Offer { get; set; } = new();
    public ServiceEndpoint Identity { get; set; } = new();
    public ServiceEndpoint Inventory { get; set; } = new();

    public sealed class ServiceEndpoint
    {
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>Timeout carved from the 300ms P95 write budget (design-decisions.md, "Resilience Pattern for Outbound Calls to Owning Services").</summary>
        public int TimeoutMilliseconds { get; set; } = 200;
    }
}
