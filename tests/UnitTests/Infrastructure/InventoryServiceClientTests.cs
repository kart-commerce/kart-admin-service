using System.Net;
using System.Net.Http.Json;
using KartAdminService.Infrastructure.ExternalClients;
using Xunit;

namespace KartAdminService.UnitTests.Infrastructure;

/// <summary>
/// Inventory & Stock Management flow's real end-to-end run found this the hard way:
/// InventoryServiceClient.ReplenishAsync posted to "/v1/inventory/{sku}/replenish" (sku as a path
/// segment), but kart-inventory-service's own InventoryController only ever maps
/// "POST /v1/inventory/replenish" with sku as a body field - every real replenish call through
/// this proxy has therefore always 404'd (masked as a generic downstream "not_found"), never
/// caught because every existing test stands in a FakeInventoryServiceClient rather than
/// exercising the real HTTP client's request shape (same class of gap as
/// kart-order-service's own InventoryClientTests). These tests assert the exact request
/// URI/body so this exact regression can't silently reappear.
/// </summary>
public sealed class InventoryServiceClientTests
{
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }
        public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(ResponseStatusCode) { Content = JsonContent.Create(new { }) };
        }
    }

    private static (InventoryServiceClient Client, RecordingHandler Handler) CreateClient()
    {
        var handler = new RecordingHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://inventory:8080") };
        return (new InventoryServiceClient(httpClient), handler);
    }

    [Fact]
    public async Task ReplenishAsync_PostsToTheRealInventoryReplenishPath_WithSkuInTheBody()
    {
        var (client, handler) = CreateClient();

        await client.ReplenishAsync("SKU-1", "WH-1", 10, "restock", "idem-1", CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("/v1/inventory/replenish", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Contains("\"sku\":\"SKU-1\"", handler.LastRequestBody);
        Assert.Contains("\"warehouseId\":\"WH-1\"", handler.LastRequestBody);
    }

    [Fact]
    public async Task ProvisionAsync_PostsToTheRealInventoryProvisionPath()
    {
        var (client, handler) = CreateClient();

        await client.ProvisionAsync("WH-1", "SKU-NEW", 10, 2, 20, CancellationToken.None);

        Assert.Equal("/v1/inventory/provision", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateThresholdAsync_PatchesTheRealInventoryThresholdPath()
    {
        var (client, handler) = CreateClient();

        await client.UpdateThresholdAsync("WH-1", "SKU-1", 5, 50, CancellationToken.None);

        Assert.Equal(HttpMethod.Patch, handler.LastRequest!.Method);
        Assert.Equal("/v1/inventory/WH-1/SKU-1/threshold", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ReconcileAsync_PostsToTheRealInventoryReconcilePath()
    {
        var (client, handler) = CreateClient();

        await client.ReconcileAsync("WH-1", "SKU-1", 30, "cycle count", CancellationToken.None);

        Assert.Equal("/v1/inventory/WH-1/SKU-1/reconcile", handler.LastRequest!.RequestUri!.AbsolutePath);
    }
}
