using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;

namespace KartAdminService.ContractTests;

/// <summary>api-contract.yaml /admin/products* (ADM-4, ADM-5, ADM-6).</summary>
public sealed class ProductsContractTests : IClassFixture<AdminContractTestFactory>
{
    private readonly AdminContractTestFactory _factory;

    public ProductsContractTests(AdminContractTestFactory factory)
    {
        _factory = factory;
        _factory.GrantRepository.Grants.Add(AdminPermissionGrant.Issue("test-admin", PermissionCategory.CatalogManagement, "seed-script", DateTimeOffset.UtcNow).Value);
    }

    [Fact]
    public async Task PostProducts_AsAuthorizedAdmin_Returns201()
    {
        _factory.ProductClient.CreateResult = Result.Success("product-42");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/products", new { name = "Widget", categoryId = "cat-1", sku = "widget-sku-1", price = new { amount = 9.99, currency = "USD" } });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ActionBody>();
        body!.EntityId.Should().Be("product-42");
        body.Action.Should().Be(ActionNames.ProductCreate);
    }

    [Fact]
    public async Task PostProducts_WhenProductServiceIsUnavailable_Returns503()
    {
        _factory.ProductClient.CreateResult = Result.Failure<string>(Error.Custom("downstream_unavailable", "down"));
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/products", new { name = "Widget", categoryId = "cat-1", sku = "widget-sku-1", price = new { amount = 9.99, currency = "USD" } });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task PutProduct_ForwardsIfMatch_AndReturns200()
    {
        _factory.ProductClient.UpdateResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("If-Match", "\"v2\"");

        var response = await client.PutAsJsonAsync("/v1/admin/products/product-1", new { name = "Widget v2", categoryId = "cat-1", price = new { amount = 12.99, currency = "USD" } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeactivateProduct_WhenNotFound_Returns404()
    {
        _factory.ProductClient.DeactivateResult = Result.Failure(Error.NotFound("no such product"));
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsync("/v1/admin/products/missing/deactivate", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostProducts_WithoutCatalogManagementGrant_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("X-Test-Sub", "unprivileged");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/products", new { name = "Widget", categoryId = "cat-1", sku = "widget-sku-1", price = new { amount = 9.99, currency = "USD" } });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record ActionBody(Guid ActionId, string EntityId, string Action);
}
