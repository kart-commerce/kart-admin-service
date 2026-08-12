using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;

namespace KartAdminService.ContractTests;

/// <summary>api-contract.yaml /admin/attributes* - added for the "Category &amp; Attribute Management (Admin)" flow, mirroring CategoriesContractTests' own shape.</summary>
public sealed class AttributesContractTests : IClassFixture<AdminContractTestFactory>
{
    private readonly AdminContractTestFactory _factory;

    public AttributesContractTests(AdminContractTestFactory factory)
    {
        _factory = factory;
        _factory.GrantRepository.Grants.Add(AdminPermissionGrant.Issue("test-admin", PermissionCategory.CatalogManagement, "seed-script", DateTimeOffset.UtcNow).Value);
    }

    [Fact]
    public async Task PostAttributes_AsAuthorizedAdmin_Returns201()
    {
        _factory.AttributeClient.CreateResult = Result.Success("attribute-7");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/attributes", new { name = "Color", categoryId = (string?)null, dataType = "select", values = new[] { new { value = "Red", displayOrder = 0 } } });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ActionBody>();
        body!.EntityId.Should().Be("attribute-7");
    }

    [Fact]
    public async Task PutAttribute_Returns200()
    {
        _factory.AttributeClient.UpdateResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PutAsJsonAsync("/v1/admin/attributes/attribute-1", new { name = "Primary Color", values = new[] { new { value = "Red", displayOrder = 0 } } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteAttribute_Returns200()
    {
        _factory.AttributeClient.DeprecateResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.DeleteAsync("/v1/admin/attributes/attribute-1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostAttributes_WhenCategoryServiceIsUnavailable_Returns503()
    {
        _factory.AttributeClient.CreateResult = Result.Failure<string>(Error.Custom("downstream_unavailable", "down"));
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/attributes", new { name = "Warranty period", categoryId = (string?)null, dataType = "text", values = Array.Empty<object>() });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    private sealed record ActionBody(Guid ActionId, string EntityId);
}
