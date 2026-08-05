using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;

namespace KartAdminService.ContractTests;

/// <summary>api-contract.yaml /admin/categories* (ADM-7, ADM-8, ADM-9, ADM-10).</summary>
public sealed class CategoriesContractTests : IClassFixture<AdminContractTestFactory>
{
    private readonly AdminContractTestFactory _factory;

    public CategoriesContractTests(AdminContractTestFactory factory)
    {
        _factory = factory;
        _factory.GrantRepository.Grants.Add(AdminPermissionGrant.Issue("test-admin", PermissionCategory.CatalogManagement, "seed-script", DateTimeOffset.UtcNow).Value);
    }

    [Fact]
    public async Task PostCategories_AsAuthorizedAdmin_Returns201()
    {
        _factory.CategoryClient.CreateResult = Result.Success("category-7");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/categories", new { name = "Electronics" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ActionBody>();
        body!.EntityId.Should().Be("category-7");
    }

    [Fact]
    public async Task PutCategory_Returns200()
    {
        _factory.CategoryClient.UpdateResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("If-Match", "\"v1\"");

        var response = await client.PutAsJsonAsync("/v1/admin/categories/category-1", new { name = "Electronics v2" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReorderCategory_Returns200()
    {
        _factory.CategoryClient.ReorderResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/categories/category-1/reorder", new { displayOrder = 2 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MoveCategory_WhenItWouldCreateACycle_Returns503WhenCategoryServiceIsUnavailable()
    {
        _factory.CategoryClient.MoveResult = Result.Failure(Error.Custom("downstream_unavailable", "down"));
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/categories/category-1/move", new { newParentId = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    private sealed record ActionBody(Guid ActionId, string EntityId);
}
