using System.Net.Http.Json;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;

namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>
/// Calls Product Service's own write API (architecture.md Dependencies table, ADR-0010
/// Decision 2). Wired against Product's now-shipped contracts/api-contract.yaml: what Admin
/// calls "a product" here is the Product-group (parent) aggregate, so create/update/deactivate
/// all target `/v1/product-groups[/{id}]`, which is GUID-keyed - but Admin's own
/// /admin/products/{sku} contract only ever knows the SKU, so update/deactivate first resolve it
/// via <see cref="GetProductGroupIdAsync"/> (`GET /v1/products/{sku}`, the separate Variant/SKU
/// resource, Product's own `ProductsController`) before calling through with only the fields on
/// <see cref="ProductWriteRequest"/>. Product's write endpoints take no Idempotency-Key/If-Match
/// header (its own contract note: caller-supplied SKU uniqueness is its idempotency backstop, and
/// it has no ETag/version concept at all) - both are still sent here since Product ignores
/// unrecognized headers and Admin's own ADM-4/ADM-5 contract still requires them from its
/// callers; If-Match in particular currently forwards to nothing that checks it, which is a real
/// gap in Admin's documented 409-on-stale-version behavior (edge-cases.md "Concurrent Admins
/// Editing the Same Back-Office Record") until Product ships its own optimistic-concurrency
/// mechanism or Admin's contract drops that guarantee for this resource.
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

    // GET /v1/products/{sku} (the Variant read path) is the only place a SKU resolves to its
    // parent Product-group's id - PATCH /v1/product-groups/{productGroupId} below is GUID-keyed,
    // but everything upstream of this client (admin-web's form, the /admin/products/{sku}
    // contract) only ever knows the SKU. No Idempotency-Key/If-Match: this is a plain read.
    public Task<Result<string>> GetProductGroupIdAsync(string sku, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Get, $"/v1/products/{sku}", body: null, idempotencyKey: null, ifMatch: null),
            static async (response, ct) =>
            {
                var product = await response.Content.ReadFromJsonAsync<ProductGroupIdLookupResponse>(cancellationToken: ct);
                return product?.ProductGroupId.ToString() ?? string.Empty;
            },
            "Product Service",
            cancellationToken);

    public Task<Result> UpdateProductAsync(string productGroupId, ProductWriteRequest request, string ifMatch, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Patch, $"/v1/product-groups/{productGroupId}", ToUpdateBody(request), idempotencyKey, ifMatch),
            "Product Service",
            cancellationToken);

    // PATCH /v1/products/{sku} (the Variant resource, SKU-keyed) - the only place a price change
    // actually applies. UpdateVariantCommandHandler on Product's side rejects a body that mixes
    // price with status/attributes, so this is deliberately its own call with a price-only body.
    public Task<Result> UpdatePriceAsync(string sku, MoneyDto price, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Patch, $"/v1/products/{sku}", new { price = new { amount = price.Amount, currency = price.Currency } }, idempotencyKey, ifMatch: null),
            "Product Service",
            cancellationToken);

    public Task<Result> DeactivateProductAsync(string productGroupId, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Patch, $"/v1/product-groups/{productGroupId}", new { status = "Archived" }, idempotencyKey, ifMatch: null),
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

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, string? idempotencyKey, string? ifMatch)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        if (ifMatch is not null)
        {
            // TryAddWithoutValidation, not Add: If-Match's typed header parser only accepts a
            // quoted ETag (or `*`), and admin-web sends ProductResponse.lastUpdatedAt's raw
            // ISO-8601 timestamp as this value (the "no real version token" substitute this
            // class's own doc comment above already flags). Add() throws FormatException on
            // that shape - a real bug that 500'd every single product update - and since Product
            // Service ignores this header entirely (same doc comment), there is no ETag
            // semantics to preserve here; just forward the byte value untouched.
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        return _httpClient.SendAsync(request);
    }

    private sealed record ProductGroupCreatedResponse(string ProductGroupId);

    private sealed record ProductGroupIdLookupResponse(Guid ProductGroupId);
}
