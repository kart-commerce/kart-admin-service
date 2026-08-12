using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;

namespace KartAdminService.Application.Common.Interfaces;

/// <summary>
/// Adapter wrapping Order Service's own admin-gated write API (architecture.md Dependencies
/// table pattern, same shape as ICategoryServiceClient/IProductServiceClient). Order Management
/// (Admin) flow #7 — every method here targets an endpoint on kart-order-service's `OrdersController`
/// that is itself `[Authorize(Policy = AdminOnlyPolicy)]`-gated, called with this service's own
/// client-credentials principal (ServicePrincipalAuthHandler), exactly like
/// resolve-fulfillment-exception already was before this flow — the difference is every call now
/// also passes through AdminActionExecutor here first, so it gets a real admin_actions audit row.
/// </summary>
public interface IOrderServiceClient
{
    Task<Result> CancelOrderAsync(Guid orderId, string? reason, string idempotencyKey, CancellationToken cancellationToken);

    Task<Result> UpdateStatusAsync(Guid orderId, string targetStatus, string reason, string idempotencyKey, CancellationToken cancellationToken);

    Task<Result> UpdateShippingAddressAsync(Guid orderId, ShippingAddressWriteRequest address, string idempotencyKey, CancellationToken cancellationToken);

    Task<Result> RequestShipmentAsync(Guid orderId, string idempotencyKey, CancellationToken cancellationToken);

    Task<Result> ResolveFulfillmentExceptionAsync(Guid orderId, string action, string idempotencyKey, CancellationToken cancellationToken);
}
