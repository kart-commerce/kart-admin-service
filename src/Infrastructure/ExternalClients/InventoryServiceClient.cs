using System.Net.Http.Json;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;

namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>
/// Calls Inventory Service's own replenishment write path (architecture.md Dependencies table —
/// the same path kart-inventory-service/requirement-spec.md §6 Decision 5's threshold-based
/// automated trigger uses). Inventory itself publishes InventoryReplenished; Admin does not.
///
/// Inventory & Stock Management flow fix (2026-08-12): SendAsync previously posted to
/// `/v1/inventory/{sku}/replenish` (sku as a path segment), but kart-inventory-service's own
/// `InventoryController` only ever mapped `POST /v1/inventory/replenish` with `sku` as a body
/// field (`ReplenishStockRequest(WarehouseId, Sku, QtyAdded)`) - no `{sku}` path segment exists
/// on that side at all. Every real replenish call through this proxy has therefore always 404'd
/// (masked as a generic downstream `not_found`, same class of regression as flow #7's
/// `/inventory/reserve` no-`/v1`-prefix bug) - found live during this flow's own end-to-end test.
/// </summary>
public sealed class InventoryServiceClient : IInventoryServiceClient
{
    private readonly HttpClient _httpClient;

    public InventoryServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<Result> ReplenishAsync(string sku, string warehouseId, int qtyAdded, string? reason, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(sku, warehouseId, qtyAdded, reason, idempotencyKey),
            "Inventory Service",
            cancellationToken);

    /// <summary>Onboards a brand-new (warehouseId, sku) row (Inventory & Stock Management flow) - Inventory's own endpoint already 400s on a duplicate, so no separate idempotency header is needed beyond admin-service's own admin_actions replay guard.</summary>
    public Task<Result> ProvisionAsync(string warehouseId, string sku, int initialQty, int replenishmentThreshold, int targetStockingLevel, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => _httpClient.PostAsJsonAsync("/v1/inventory/provision", new { warehouseId, sku, initialQty, replenishmentThreshold, targetStockingLevel }, cancellationToken),
            "Inventory Service",
            cancellationToken);

    public Task<Result> UpdateThresholdAsync(string warehouseId, string sku, int replenishmentThreshold, int targetStockingLevel, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => _httpClient.PatchAsJsonAsync($"/v1/inventory/{warehouseId}/{sku}/threshold", new { replenishmentThreshold, targetStockingLevel }, cancellationToken),
            "Inventory Service",
            cancellationToken);

    public Task<Result> ReconcileAsync(string warehouseId, string sku, int countedQty, string reason, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => _httpClient.PostAsJsonAsync($"/v1/inventory/{warehouseId}/{sku}/reconcile", new { countedQty, reason }, cancellationToken),
            "Inventory Service",
            cancellationToken);

    private Task<HttpResponseMessage> SendAsync(string sku, string warehouseId, int qtyAdded, string? reason, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/inventory/replenish")
        {
            Content = JsonContent.Create(new { warehouseId, sku, qtyAdded, reason }),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return _httpClient.SendAsync(request);
    }
}
