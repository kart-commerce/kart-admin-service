using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;

namespace KartAdminService.ContractTests;

/// <summary>api-contract.yaml /admin/inventory/{sku}/replenish (ADM-15).</summary>
public sealed class InventoryContractTests : IClassFixture<AdminContractTestFactory>
{
    private readonly AdminContractTestFactory _factory;

    public InventoryContractTests(AdminContractTestFactory factory)
    {
        _factory = factory;
        _factory.GrantRepository.Grants.Add(AdminPermissionGrant.Issue("test-admin", PermissionCategory.InventoryReplenishment, "seed-script", DateTimeOffset.UtcNow).Value);
    }

    [Fact]
    public async Task ReplenishInventory_AsAuthorizedAdmin_Returns200()
    {
        _factory.InventoryClient.ReplenishResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/inventory/sku-1/replenish", new { warehouseId = "warehouse-1", qtyAdded = 50 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ActionBody>();
        body!.EntityId.Should().Be("sku-1");
    }

    [Fact]
    public async Task ReplenishInventory_WhenSkuNotFound_Returns404()
    {
        _factory.InventoryClient.ReplenishResult = Result.Failure(Error.NotFound("no such sku"));
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/inventory/missing/replenish", new { warehouseId = "warehouse-1", qtyAdded = 50 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record ActionBody(Guid ActionId, string EntityId);
}
