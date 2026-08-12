using System.Net.Http.Json;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;

namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>
/// Calls Category Service's own Attribute write API (v1/attributes) - added for the "Category &amp;
/// Attribute Management (Admin)" flow. Attribute is a second aggregate inside kart-category-service
/// itself, not a separate microservice, so this typed HttpClient is registered against the exact
/// same base address as ICategoryServiceClient (see DependencyInjection.AddDownstreamClients) -
/// there is deliberately no separate "Attribute" entry in DownstreamServiceOptions.
/// </summary>
public sealed class AttributeServiceClient : IAttributeServiceClient
{
    private readonly HttpClient _httpClient;

    public AttributeServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<Result<string>> CreateAttributeAsync(AttributeWriteRequest request, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Post, "/v1/attributes", request, idempotencyKey, ifMatch: null),
            static async (response, ct) =>
            {
                var created = await response.Content.ReadFromJsonAsync<AttributeIdResponse>(cancellationToken: ct);
                return created?.AttributeId ?? string.Empty;
            },
            "Category Service",
            cancellationToken);

    public Task<Result> UpdateAttributeAsync(string attributeId, AttributeUpdateRequest request, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Patch, $"/v1/attributes/{attributeId}", request, idempotencyKey, ifMatch: null),
            "Category Service",
            cancellationToken);

    public Task<Result> DeprecateAttributeAsync(string attributeId, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Delete, $"/v1/attributes/{attributeId}", body: null, idempotencyKey, ifMatch: null),
            "Category Service",
            cancellationToken);

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, string idempotencyKey, string? ifMatch)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        request.Headers.Add("Idempotency-Key", idempotencyKey);
        if (ifMatch is not null)
        {
            request.Headers.Add("If-Match", ifMatch);
        }

        return _httpClient.SendAsync(request);
    }

    private sealed record AttributeIdResponse(string AttributeId);
}
