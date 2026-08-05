using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;

namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>
/// Calls kart-identity-service's real, confirmed internal routes —
/// POST /v1/internal/users/{userId}/lock|unlock (Kart.Identity.Api.Endpoints.InternalUserEndpoints),
/// gated by the `scopes: admin` claim only a client-credentials token carries (never a role
/// check, which wouldn't distinguish this from an interactive Admin user's own bearer token).
/// </summary>
public sealed class IdentityAdminClient : IIdentityAdminClient
{
    private readonly HttpClient _httpClient;
    private readonly IdentityClientCredentialsTokenProvider _tokenProvider;

    public IdentityAdminClient(HttpClient httpClient, IdentityClientCredentialsTokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
    }

    public Task<Result> LockUserAsync(string userId, string? reason, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Post, $"/v1/internal/users/{userId}/lock", reason is null ? null : new { reason }, idempotencyKey, cancellationToken),
            "Identity Service",
            cancellationToken);

    public Task<Result> UnlockUserAsync(string userId, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Post, $"/v1/internal/users/{userId}/unlock", body: null, idempotencyKey, cancellationToken),
            "Identity Service",
            cancellationToken);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, string idempotencyKey, CancellationToken cancellationToken)
    {
        var accessToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken);

        var request = new HttpRequestMessage(method, path)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) },
        };
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await _httpClient.SendAsync(request, cancellationToken);
    }
}
