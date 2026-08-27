using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.ProvisionWarehouseStock;

/// <summary>Inventory & Stock Management flow: onboards a brand-new (warehouseId, sku) row. Category `inventory-replenishment`.</summary>
public sealed record ProvisionWarehouseStockCommand(
    string ActingPrincipalId,
    string WarehouseId,
    string Sku,
    int InitialQty,
    int ReplenishmentThreshold,
    int TargetStockingLevel,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
