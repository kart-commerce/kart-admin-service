namespace KartAdminService.Application.Common.Models;

/// <summary>
/// api-contract.yaml ProductWriteRequest — best-effort mirror of Product Service's own
/// POST /products / PUT /products/{id} request shape (final shape is Product's own API Design
/// Agent's job; this client is written against the documented contract and will need
/// reconciling once that service ships its real endpoint — see api-contract.yaml's header note).
///
/// Price is nullable, not required: this one DTO is shared by both Create (Price required,
/// enforced by CreateProductCommandValidator) and Update (Price never sent at all -
/// ProductServiceClient.ToUpdateBody only forwards name/description/categoryId; price lives on
/// Product's own Variant resource, PATCH /v1/products/{sku}, a call this DTO never makes).
/// Previously non-nullable, which meant [ApiController]'s automatic model-binding validation
/// rejected every PUT /admin/products/{id} request with "The Price field is required" before
/// UpdateProductCommandValidator (which correctly never required it) ever ran - a real bug this
/// session's end-to-end verification of the Update stage surfaced and fixed.
/// </summary>
public sealed record ProductWriteRequest(string Name, string? Description, string CategoryId, MoneyDto? Price, string? Sku);

/// <summary>api-contract.yaml CategoryWriteRequest — best-effort mirror of Category Service's own write API.</summary>
public sealed record CategoryWriteRequest(string Name, string? ParentId, int? DisplayOrder);

/// <summary>
/// api-contract.yaml AttributeWriteRequest — mirrors Category Service's own POST /v1/attributes
/// (Attribute is a second aggregate in that same service, not a separate one - see
/// AttributeServiceClient's doc comment). CategoryId null creates a global attribute.
/// </summary>
public sealed record AttributeWriteRequest(string Name, string? CategoryId, string DataType, IReadOnlyList<AttributeValueWriteRequest>? Values);

/// <summary>api-contract.yaml AttributeUpdateRequest — Category Service's PATCH /v1/attributes/{id}; CategoryId/DataType are immutable after creation so this DTO omits them entirely.</summary>
public sealed record AttributeUpdateRequest(string Name, IReadOnlyList<AttributeValueWriteRequest>? Values);

/// <summary>api-contract.yaml AttributeValueWriteRequest.</summary>
public sealed record AttributeValueWriteRequest(string Value, int DisplayOrder);

/// <summary>api-contract.yaml CouponWriteRequest — best-effort mirror of Offer Service's own POST /coupons.</summary>
public sealed record CouponWriteRequest(
    string CouponCode,
    MoneyDto DiscountValue,
    int? PerUserCap,
    int? GlobalCap,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil);
