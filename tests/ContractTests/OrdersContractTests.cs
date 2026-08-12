using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;

namespace KartAdminService.ContractTests;

/// <summary>api-contract.yaml /admin/orders* (Order Management (Admin) flow #7).</summary>
public sealed class OrdersContractTests : IClassFixture<AdminContractTestFactory>
{
    private readonly AdminContractTestFactory _factory;

    public OrdersContractTests(AdminContractTestFactory factory)
    {
        _factory = factory;
        _factory.GrantRepository.Grants.Add(AdminPermissionGrant.Issue("test-admin", PermissionCategory.OrderManagement, "seed-script", DateTimeOffset.UtcNow).Value);
    }

    [Fact]
    public async Task CancelOrder_AsAuthorizedAdmin_Returns200()
    {
        _factory.OrderClient.CancelResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var orderId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync($"/v1/admin/orders/{orderId}/cancel", new { reason = "customer request" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ActionBody>();
        body!.EntityId.Should().Be(orderId.ToString());
        body.Category.Should().Be("order-management");
    }

    [Fact]
    public async Task CancelOrder_WithNoGrant_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        // A different acting principal than the one seeded with a live grant in the ctor.
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.SubHeader, "unauthorized-admin");

        var response = await client.PostAsJsonAsync($"/v1/admin/orders/{Guid.NewGuid()}/cancel", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateOrderStatus_Returns200()
    {
        _factory.OrderClient.UpdateStatusResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PatchAsJsonAsync($"/v1/admin/orders/{Guid.NewGuid()}/status", new { targetStatus = "Shipped", reason = "courier confirmed out-of-band" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateShippingAddress_Returns200()
    {
        _factory.OrderClient.UpdateShippingAddressResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PatchAsJsonAsync($"/v1/admin/orders/{Guid.NewGuid()}/shipping-address", new
        {
            recipientName = "Jane Doe",
            line1 = "1 Test St",
            line2 = (string?)null,
            city = "Testville",
            state = "TS",
            postalCode = "00000",
            country = "US",
            phone = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RequestShipment_Returns200()
    {
        _factory.OrderClient.RequestShipmentResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsync($"/v1/admin/orders/{Guid.NewGuid()}/request-shipment", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResolveFulfillmentException_Returns200()
    {
        _factory.OrderClient.ResolveFulfillmentExceptionResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync($"/v1/admin/orders/{Guid.NewGuid()}/resolve-fulfillment-exception", new { action = "retry" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RequestShipment_WhenOrderServiceRejects_Returns409()
    {
        _factory.OrderClient.RequestShipmentResult = Result.Failure(Error.Conflict("Order must be 'Paid' to request shipment."));
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsync($"/v1/admin/orders/{Guid.NewGuid()}/request-shipment", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private sealed record ActionBody(Guid ActionId, string EntityId, string Category);
}
