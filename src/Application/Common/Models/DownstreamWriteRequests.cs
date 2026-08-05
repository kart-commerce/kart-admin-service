namespace KartAdminService.Application.Common.Models;

/// <summary>
/// api-contract.yaml ProductWriteRequest — best-effort mirror of Product Service's own
/// POST /products / PUT /products/{id} request shape (final shape is Product's own API Design
/// Agent's job; this client is written against the documented contract and will need
/// reconciling once that service ships its real endpoint — see api-contract.yaml's header note).
/// </summary>
public sealed record ProductWriteRequest(string Name, string? Description, string CategoryId, MoneyDto Price, string? Sku);

/// <summary>api-contract.yaml CategoryWriteRequest — best-effort mirror of Category Service's own write API.</summary>
public sealed record CategoryWriteRequest(string Name, string? ParentId, int? DisplayOrder);

/// <summary>api-contract.yaml CouponWriteRequest — best-effort mirror of Offer Service's own POST /coupons.</summary>
public sealed record CouponWriteRequest(
    string CouponCode,
    MoneyDto DiscountValue,
    int? PerUserCap,
    int? GlobalCap,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil);
