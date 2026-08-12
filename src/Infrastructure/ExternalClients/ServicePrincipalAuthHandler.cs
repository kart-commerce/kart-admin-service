using System.Net.Http.Headers;

namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>
/// Attaches the Bearer token this service authenticates its own downstream write calls with
/// (Product/Category/Offer/Inventory Service, per architecture.md's Dependencies table) - a
/// shared DelegatingHandler so every downstream typed HttpClient gets it for free instead of
/// re-implementing the same few lines per client. Before this handler existed,
/// ProductServiceClient/CategoryServiceClient/OfferServiceClient/InventoryServiceClient sent no
/// Authorization header at all - a real gap, not an intentional omission - while IdentityAdminClient
/// was the one client that already did this correctly (per-call, not via a shared handler). Reuses
/// the same IdentityClientCredentialsTokenProvider that client uses, so the token is fetched once
/// and cached/refreshed across every downstream client that wires this handler in.
/// </summary>
public sealed class ServicePrincipalAuthHandler(IdentityClientCredentialsTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
