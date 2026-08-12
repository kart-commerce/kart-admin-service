using Kart.Shared.Domain;

namespace KartAdminService.Application.Common.Interfaces;

/// <summary>
/// Adapter wrapping Inventory Service's own write paths (architecture.md Dependencies table).
/// ReplenishAsync backs ADM-15 (pre-existing); ProvisionAsync/UpdateThresholdAsync/ReconcileAsync
/// were added for the Inventory &amp; Stock Management flow's onboarding/threshold/audit-reconciliation
/// admin write paths.
/// </summary>
public interface IInventoryServiceClient
{
    Task<Result> ReplenishAsync(string sku, string warehouseId, int qtyAdded, string? reason, string idempotencyKey, CancellationToken cancellationToken);

    Task<Result> ProvisionAsync(string warehouseId, string sku, int initialQty, int replenishmentThreshold, int targetStockingLevel, CancellationToken cancellationToken);

    Task<Result> UpdateThresholdAsync(string warehouseId, string sku, int replenishmentThreshold, int targetStockingLevel, CancellationToken cancellationToken);

    Task<Result> ReconcileAsync(string warehouseId, string sku, int countedQty, string reason, CancellationToken cancellationToken);
}
