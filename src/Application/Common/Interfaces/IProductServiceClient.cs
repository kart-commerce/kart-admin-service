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

    Task<Result> UpdateProductAsync(string productId, ProductWriteRequest request, string ifMatch, string idempotencyKey, CancellationToken cancellationToken);

    Task<Result> DeactivateProductAsync(string productId, string idempotencyKey, CancellationToken cancellationToken);
}
