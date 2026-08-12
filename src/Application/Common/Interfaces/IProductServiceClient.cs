using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;

namespace KartAdminService.Application.Common.Interfaces;

/// <summary>
/// Adapter (coding-standards.md pattern table) wrapping Product Service's own write API —
/// architecture.md's Dependencies table, ADR-0010 Decision 2. Admin never writes Product's
/// tables directly; every mutation is a synchronous, internal client-credentials call. Every
/// method forwards the caller's Idempotency-Key so a bounded retry at this layer is safe
/// (design-decisions.md, "Idempotency Mechanism for Outbound Write Calls").
/// </summary>
public interface IProductServiceClient
{
    /// <summary>Returns the new productId on success.</summary>
    Task<Result<string>> CreateProductAsync(ProductWriteRequest request, string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a SKU to its parent Product-group id via Product Service's own
    /// <c>GET /v1/products/{sku}</c> — <see cref="UpdateProductAsync"/>/<see cref="DeactivateProductAsync"/>
    /// both need this first, since kart-admin-web's /admin/products/{sku} contract is entirely
    /// SKU-keyed but Product Service's PATCH /v1/product-groups/{id} is GUID-keyed.
    /// </summary>
    Task<Result<string>> GetProductGroupIdAsync(string sku, CancellationToken cancellationToken);

    Task<Result> UpdateProductAsync(string productGroupId, ProductWriteRequest request, string ifMatch, string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>
    /// Price lives on the Variant resource, not the Product-group <see cref="UpdateProductAsync"/>
    /// targets (Product Service's own `PATCH /v1/products/{sku}` — SKU-keyed, unlike the group).
    /// Without this, admin-web's price field silently no-ops: the group PATCH above 200s having
    /// never looked at it, so a real admin sees "saved" with the old price still live.
    /// </summary>
    Task<Result> UpdatePriceAsync(string sku, MoneyDto price, string idempotencyKey, CancellationToken cancellationToken);

    Task<Result> DeactivateProductAsync(string productGroupId, string idempotencyKey, CancellationToken cancellationToken);
}
