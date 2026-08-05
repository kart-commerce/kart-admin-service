using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.ReplenishInventory;

/// <summary>api-contract.yaml POST /admin/inventory/{sku}/replenish (ADM-15). Category `inventory-replenishment`. Inventory itself publishes InventoryReplenished — Admin does not.</summary>
public sealed record ReplenishInventoryCommand(
    string ActingPrincipalId,
    string Sku,
    string WarehouseId,
    int QtyAdded,
    string? Reason,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
