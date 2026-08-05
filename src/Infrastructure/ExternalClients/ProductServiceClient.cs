using System.Net.Http.Json;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;

namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>
/// Calls Product Service's own write API (architecture.md Dependencies table, ADR-0010
/// Decision 2). Product Service has no deployed write API yet — this client is written against
/// api-contract.yaml's ProductWriteRequest mirror shape and the conventional /v1/products
/// route; reconcile against Product's own contract once it exists.
/// </summary>
public sealed class ProductServiceClient : IProductServiceClient
{
    private readonly HttpClient _httpClient;

    public ProductServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<Result<string>> CreateProductAsync(ProductWriteRequest request, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Post, "/v1/products", request, idempotencyKey, ifMatch: null),
            static async (response, ct) =>
            {
                var created = await response.Content.ReadFromJsonAsync<ProductIdResponse>(cancellationToken: ct);
                return created?.ProductId ?? string.Empty;
            },
            "Product Service",
            cancellationToken);

    public Task<Result> UpdateProductAsync(string productId, ProductWriteRequest request, string ifMatch, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Put, $"/v1/products/{productId}", request, idempotencyKey, ifMatch),
            "Product Service",
            cancellationToken);

    public Task<Result> DeactivateProductAsync(string productId, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Post, $"/v1/products/{productId}/deactivate", body: null, idempotencyKey, ifMatch: null),
            "Product Service",
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

    private sealed record ProductIdResponse(string ProductId);
}
