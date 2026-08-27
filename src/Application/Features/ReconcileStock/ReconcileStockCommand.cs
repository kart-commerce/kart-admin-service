using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.ReconcileStock;

/// <summary>Inventory & Stock Management flow's "Stock Audit/Reconciliation" and "Update Qty" stages. Category `inventory-replenishment`.</summary>
public sealed record ReconcileStockCommand(
    string ActingPrincipalId,
    string WarehouseId,
    string Sku,
    int CountedQty,
    string Reason,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
