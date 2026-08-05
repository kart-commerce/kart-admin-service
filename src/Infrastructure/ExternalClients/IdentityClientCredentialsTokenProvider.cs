using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>
/// Caches the OAuth2 Client Credentials token this service's own IdentityAdminClient calls are
/// authenticated with (kart-identity-service's POST /v1/auth/token, real and confirmed —
/// api-contract.yaml's ServicePrincipalToken). Refetches a bit before actual expiry so a
/// concurrent request never observes a token that expires mid-flight; a SemaphoreSlim collapses
/// concurrent refresh attempts into one HTTP call rather than a stampede.
/// </summary>
public sealed class IdentityClientCredentialsTokenProvider
{
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(30);

    private readonly HttpClient _httpClient;
    private readonly IdentityClientCredentialsOptions _options;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public IdentityClientCredentialsTokenProvider(HttpClient httpClient, IOptions<IdentityClientCredentialsOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt - ExpiryBuffer)
        {
            return _cachedToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt - ExpiryBuffer)
            {
                return _cachedToken;
            }

            var requestedAt = DateTimeOffset.UtcNow;
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["scope"] = _options.Scope,
            };

            using var response = await _httpClient.PostAsync("/v1/auth/token", new FormUrlEncodedContent(form), cancellationToken);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<ServicePrincipalTokenResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Identity Service returned an empty token response.");

            _cachedToken = token.AccessToken;
            _expiresAt = requestedAt.AddSeconds(token.ExpiresIn);
            return _cachedToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private sealed record ServicePrincipalTokenResponse(string AccessToken, string TokenType, int ExpiresIn);
}
