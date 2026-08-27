using Kart.Shared.Observability;
using KartAdminService.Api.Common;
using KartAdminService.Api.Security;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.ProvisionWarehouseStock;
using KartAdminService.Application.Features.ReconcileStock;
using KartAdminService.Application.Features.ReplenishInventory;
using KartAdminService.Application.Features.UpdateReplenishmentThreshold;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KartAdminService.Api.Controllers;

/// <summary>
/// api-contract.yaml /admin/inventory/* — inventory-replenishment category, proxies Inventory
/// Service's own write paths. ReplenishInventory backs ADM-15 (pre-existing); Provision/
/// UpdateThreshold/Reconcile were added for the Inventory & Stock Management flow (business-flows.md
/// flow #5). Every action here belongs to that flow.
/// </summary>
[ApiController]
[Route("v1/admin/inventory")]
[Authorize(Policy = AuthenticationExtensions.AdminPolicy)]
public sealed class InventoryController : AdminControllerBase
{
    private const string FlowName = "InventoryStockManagement";

    private readonly ISender _sender;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(ISender sender, ILogger<InventoryController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpPost("{sku}/replenish")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> ReplenishInventory(
        [FromRoute] string sku,
        [FromBody] ReplenishInventoryRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: replenish inventory for sku {Sku} received (warehouse {WarehouseId})", "AdminInventoryControllerReceived", sku, request.WarehouseId);

        var command = new ReplenishInventoryCommand(ActingPrincipalId, sku, request.WarehouseId, request.QtyAdded, request.Reason, idempotencyKey);
        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }

    /// <summary>Inventory & Stock Management flow: onboards a brand-new (warehouseId, sku) row.</summary>
    [HttpPost("provision")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminActionResultDto>> ProvisionWarehouseStock(
        [FromBody] ProvisionWarehouseStockRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: provision warehouse stock received (warehouse {WarehouseId}, sku {Sku})", "AdminInventoryControllerReceived", request.WarehouseId, request.Sku);

        var command = new ProvisionWarehouseStockCommand(ActingPrincipalId, request.WarehouseId, request.Sku, request.InitialQty, request.ReplenishmentThreshold, request.TargetStockingLevel, idempotencyKey);
        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }

    /// <summary>Inventory & Stock Management flow's "Low Stock Threshold" stage.</summary>
    [HttpPatch("{warehouseId}/{sku}/threshold")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminActionResultDto>> UpdateReplenishmentThreshold(
        [FromRoute] string warehouseId,
        [FromRoute] string sku,
        [FromBody] UpdateReplenishmentThresholdRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: update replenishment threshold received (warehouse {WarehouseId}, sku {Sku})", "AdminInventoryControllerReceived", warehouseId, sku);

        var command = new UpdateReplenishmentThresholdCommand(ActingPrincipalId, warehouseId, sku, request.ReplenishmentThreshold, request.TargetStockingLevel, idempotencyKey);
        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }

    /// <summary>Inventory & Stock Management flow's "Stock Audit/Reconciliation" and "Update Qty" stages.</summary>
    [HttpPost("{warehouseId}/{sku}/reconcile")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminActionResultDto>> ReconcileStock(
        [FromRoute] string warehouseId,
        [FromRoute] string sku,
        [FromBody] ReconcileStockRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: reconcile stock received (warehouse {WarehouseId}, sku {Sku})", "AdminInventoryControllerReceived", warehouseId, sku);

        var command = new ReconcileStockCommand(ActingPrincipalId, warehouseId, sku, request.CountedQty, request.Reason, idempotencyKey);
        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }
}

/// <summary>api-contract.yaml replenishInventory requestBody shape.</summary>
public sealed record ReplenishInventoryRequest(string WarehouseId, int QtyAdded, string? Reason);

/// <summary>provisionWarehouseStock requestBody shape.</summary>
public sealed record ProvisionWarehouseStockRequest(string WarehouseId, string Sku, int InitialQty, int ReplenishmentThreshold, int TargetStockingLevel);

/// <summary>updateReplenishmentThreshold requestBody shape.</summary>
public sealed record UpdateReplenishmentThresholdRequest(int ReplenishmentThreshold, int TargetStockingLevel);

/// <summary>reconcileStock requestBody shape.</summary>
public sealed record ReconcileStockRequest(int CountedQty, string Reason);
