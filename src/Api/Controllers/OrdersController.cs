using Kart.Shared.Observability;
using KartAdminService.Api.Common;
using KartAdminService.Api.Security;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.AdminUpdateOrderStatus;
using KartAdminService.Application.Features.CancelOrder;
using KartAdminService.Application.Features.RequestOrderShipment;
using KartAdminService.Application.Features.ResolveOrderFulfillmentException;
using KartAdminService.Application.Features.UpdateOrderShippingAddress;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KartAdminService.Api.Controllers;

/// <summary>
/// api-contract.yaml /admin/orders* — order-management category, proxies Order Service's own
/// admin-gated write API (kart-order-service/src/Api/Controllers/OrdersController.cs). Every
/// action here belongs to the "Order Management (Admin)" flow #7 (KartFlowContext.Push mirrors
/// every sibling controller's own convention). Reads (list/detail/invoice/warehouse-allocations)
/// deliberately have no counterpart here — admin-web calls kart-order-service/kart-inventory-service
/// directly for those, same as Product/Category's own read/write split (no admin_actions audit row
/// makes sense for a read).
/// </summary>
[ApiController]
[Route("v1/admin/orders")]
[Authorize(Policy = AuthenticationExtensions.AdminPolicy)]
public sealed class OrdersController : AdminControllerBase
{
    private const string FlowName = "OrderManagementAdmin";

    private readonly ISender _sender;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ISender sender, ILogger<OrdersController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpPost("{orderId:guid}/cancel")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> CancelOrder(
        [FromRoute] Guid orderId,
        [FromBody] CancelOrderRequest? request,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: cancel order {OrderId} received", "AdminOrdersControllerReceived", orderId);

        var result = await _sender.Send(new KartAdminService.Application.Features.CancelOrder.CancelOrderCommand(ActingPrincipalId, orderId, request?.Reason, idempotencyKey), cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }

    [HttpPatch("{orderId:guid}/status")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> UpdateOrderStatus(
        [FromRoute] Guid orderId,
        [FromBody] AdminUpdateOrderStatusRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: update order {OrderId} status -> {TargetStatus} received", "AdminOrdersControllerReceived", orderId, request.TargetStatus);

        var result = await _sender.Send(new AdminUpdateOrderStatusCommand(ActingPrincipalId, orderId, request.TargetStatus, request.Reason, idempotencyKey), cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }

    [HttpPatch("{orderId:guid}/shipping-address")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> UpdateShippingAddress(
        [FromRoute] Guid orderId,
        [FromBody] ShippingAddressWriteRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: update order {OrderId} shipping address received", "AdminOrdersControllerReceived", orderId);

        var result = await _sender.Send(new UpdateOrderShippingAddressCommand(ActingPrincipalId, orderId, request, idempotencyKey), cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }

    [HttpPost("{orderId:guid}/request-shipment")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> RequestShipment(
        [FromRoute] Guid orderId,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: request shipment for order {OrderId} received", "AdminOrdersControllerReceived", orderId);

        var result = await _sender.Send(new RequestOrderShipmentCommand(ActingPrincipalId, orderId, idempotencyKey), cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }

    [HttpPost("{orderId:guid}/resolve-fulfillment-exception")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> ResolveFulfillmentException(
        [FromRoute] Guid orderId,
        [FromBody] ResolveFulfillmentExceptionRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: resolve fulfillment exception for order {OrderId} (action={Action}) received", "AdminOrdersControllerReceived", orderId, request.Action);

        var result = await _sender.Send(new ResolveOrderFulfillmentExceptionCommand(ActingPrincipalId, orderId, request.Action, idempotencyKey), cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }
}

/// <summary>api-contract.yaml cancelOrder requestBody shape — a caller may omit the body entirely.</summary>
public sealed record CancelOrderRequest(string? Reason);

/// <summary>api-contract.yaml adminUpdateOrderStatus requestBody shape.</summary>
public sealed record AdminUpdateOrderStatusRequest(string TargetStatus, string Reason);

/// <summary>api-contract.yaml resolveFulfillmentException requestBody shape ('retry' or 'cancel').</summary>
public sealed record ResolveFulfillmentExceptionRequest(string Action);
