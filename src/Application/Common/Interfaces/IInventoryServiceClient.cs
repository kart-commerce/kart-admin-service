using Kart.Shared.Domain;

namespace KartAdminService.Application.Common.Interfaces;

/// <summary>Adapter wrapping Inventory Service's own replenishment write path (architecture.md Dependencies table — same path the threshold-based automated trigger uses).</summary>
public interface IInventoryServiceClient
{
    Task<Result> ReplenishAsync(string sku, string warehouseId, int qtyAdded, string? reason, string idempotencyKey, CancellationToken cancellationToken);
}
