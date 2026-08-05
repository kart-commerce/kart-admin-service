using KartAdminService.Api.Common;
using KartAdminService.Api.Security;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.ReplenishInventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KartAdminService.Api.Controllers;

/// <summary>api-contract.yaml /admin/inventory/{sku}/replenish (ADM-15) — inventory-replenishment category, proxies Inventory Service's own replenishment write path.</summary>
[ApiController]
[Route("v1/admin/inventory")]
[Authorize(Policy = AuthenticationExtensions.AdminPolicy)]
public sealed class InventoryController : AdminControllerBase
{
    private readonly ISender _sender;

    public InventoryController(ISender sender)
    {
        _sender = sender;
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
        var command = new ReplenishInventoryCommand(ActingPrincipalId, sku, request.WarehouseId, request.QtyAdded, request.Reason, idempotencyKey);
        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }
}

/// <summary>api-contract.yaml replenishInventory requestBody shape.</summary>
public sealed record ReplenishInventoryRequest(string WarehouseId, int QtyAdded, string? Reason);
