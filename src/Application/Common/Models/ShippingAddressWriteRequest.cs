namespace KartAdminService.Application.Common.Models;

/// <summary>Mirrors kart-order-service's `PATCH /v1/orders/{id}/shipping-address` request body exactly (contracts/api-contract.yaml's UpdateShippingAddressRequest there).</summary>
public sealed record ShippingAddressWriteRequest(
    string RecipientName,
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country,
    string? Phone);
