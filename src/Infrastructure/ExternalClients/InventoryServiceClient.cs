using System.Net.Http.Json;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;

namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>
/// Calls Inventory Service's own replenishment write path (architecture.md Dependencies table —
/// the same path kart-inventory-service/requirement-spec.md §6 Decision 5's threshold-based
/// automated trigger uses). Inventory itself publishes InventoryReplenished; Admin does not.
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

    private Task<HttpResponseMessage> SendAsync(string sku, string warehouseId, int qtyAdded, string? reason, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/inventory/{sku}/replenish")
        {
            Content = JsonContent.Create(new { warehouseId, qtyAdded, reason }),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return _httpClient.SendAsync(request);
    }
}
