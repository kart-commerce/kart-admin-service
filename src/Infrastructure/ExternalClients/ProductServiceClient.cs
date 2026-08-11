using System.Net.Http.Json;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;

namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>
/// Calls Product Service's own write API (architecture.md Dependencies table, ADR-0010
/// Decision 2). Wired against Product's now-shipped contracts/api-contract.yaml: what Admin
/// calls "a product" here is the Product-group (parent) aggregate, so create/update/deactivate
/// all target `/v1/product-groups[/{id}]` - never `/v1/products/{sku}`, which addresses the
/// separate Variant/SKU resource (Product's own `ProductsController`) and isn't reachable with
/// only the fields on <see cref="ProductWriteRequest"/>. Product's write endpoints take no
/// Idempotency-Key/If-Match header (its own contract note: caller-supplied SKU uniqueness is
/// its idempotency backstop, and it has no ETag/version concept at all) - both are still sent
/// here since Product ignores unrecognized headers and Admin's own ADM-4/ADM-5 contract still
/// requires them from its callers; If-Match in particular currently forwards to nothing that
/// checks it, which is a real gap in Admin's documented 409-on-stale-version behavior (edge-
/// cases.md "Concurrent Admins Editing the Same Back-Office Record") until Product ships its own
/// optimistic-concurrency mechanism or Admin's contract drops that guarantee for this resource.
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
            () => SendAsync(HttpMethod.Post, "/v1/product-groups", ToCreateBody(request), idempotencyKey, ifMatch: null),
            static async (response, ct) =>
            {
                var created = await response.Content.ReadFromJsonAsync<ProductGroupCreatedResponse>(cancellationToken: ct);
                return created?.ProductGroupId ?? string.Empty;
            },
            "Product Service",
            cancellationToken);

    public Task<Result> UpdateProductAsync(string productId, ProductWriteRequest request, string ifMatch, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Patch, $"/v1/product-groups/{productId}", ToUpdateBody(request), idempotencyKey, ifMatch),
            "Product Service",
            cancellationToken);

    public Task<Result> DeactivateProductAsync(string productId, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Patch, $"/v1/product-groups/{productId}", new { status = "Archived" }, idempotencyKey, ifMatch: null),
            "Product Service",
            cancellationToken);

    // api-contract.yaml POST /v1/product-groups requires name/categoryId/sku/price. brand and
    // attributes are accepted there too but aren't on ProductWriteRequest yet, so they're never
    // sent - both are optional on Product's side, so this is a narrower create, not a broken one.
    private static object ToCreateBody(ProductWriteRequest request) => new
    {
        name = request.Name,
        description = request.Description,
        categoryId = request.CategoryId,
        sku = request.Sku,
        // Null-forgiving: CreateProductCommandValidator.RuleFor(x => x.Product.Price).NotNull()
        // already rejects a null Price before this client is ever called for a create.
        price = new { amount = request.Price!.Amount, currency = request.Price.Currency },
    };

    // api-contract.yaml PATCH /v1/product-groups/{id} edits parent fields only - price lives on
    // the Variant resource (PATCH /v1/products/{sku}), a call this client doesn't make.
    private static object ToUpdateBody(ProductWriteRequest request) => new
    {
        name = request.Name,
        description = request.Description,
        categoryId = request.CategoryId,
    };

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

    private sealed record ProductGroupCreatedResponse(string ProductGroupId);
}
