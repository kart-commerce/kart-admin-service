using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.ResolveOrderFulfillmentException;

/// <summary>
/// api-contract.yaml POST /admin/orders/{orderId}/resolve-fulfillment-exception (Order Management
/// (Admin) flow #7's "Handle Escalation"). Before this flow, admin-web called kart-order-service's
/// own AdminOnly-gated resolve-fulfillment-exception endpoint directly (bypassing this service
/// entirely, the gateway's generic `/v1/orders/{**catch-all}` route) — real bugs get fixed, and a
/// zero-audit-trail escalation-resolution path on an "Order Management (Admin)" flow is exactly
/// that: routing it through here means every resolve now gets a real admin_actions row, same as
/// every other admin write on this platform.
/// </summary>
public sealed record ResolveOrderFulfillmentExceptionCommand(
    string ActingPrincipalId,
    Guid OrderId,
    string Action,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
