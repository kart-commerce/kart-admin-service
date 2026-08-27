using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.UpdateReplenishmentThreshold;

/// <summary>Inventory & Stock Management flow's "Low Stock Threshold" stage. Category `inventory-replenishment`.</summary>
public sealed record UpdateReplenishmentThresholdCommand(
    string ActingPrincipalId,
    string WarehouseId,
    string Sku,
    int ReplenishmentThreshold,
    int TargetStockingLevel,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
