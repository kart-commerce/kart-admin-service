using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;

namespace KartAdminService.ContractTests;

/// <summary>api-contract.yaml /admin/permission-grants* (ADM-1, ADM-2, ADM-3).</summary>
public sealed class PermissionGrantsContractTests : IClassFixture<AdminContractTestFactory>
{
    private readonly AdminContractTestFactory _factory;

    public PermissionGrantsContractTests(AdminContractTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostPermissionGrants_AsAPrincipalManagementHolder_Returns201WithTheIssuedGrant()
    {
        _factory.GrantRepository.Grants.Add(AdminPermissionGrant.Issue("test-admin", PermissionCategory.PermissionManagement, "seed-script", DateTimeOffset.UtcNow).Value);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/permission-grants", new { principalId = "new-admin", category = "catalog-management" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<GrantBody>();
        body!.PrincipalId.Should().Be("new-admin");
        body.Category.Should().Be("catalog-management");
    }

    [Fact]
    public async Task PostPermissionGrants_WithoutAPermissionManagementGrant_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("X-Test-Sub", "no-grants-admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/permission-grants", new { principalId = "someone", category = "catalog-management" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostPermissionGrants_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/permission-grants", new { principalId = "someone", category = "catalog-management" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPermissionGrants_ReturnsThePagedEnvelope()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");

        var response = await client.GetAsync("/v1/admin/permission-grants");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedBody>();
        body!.Page.Should().Be(1);
    }

    [Fact]
    public async Task RevokePermissionGrant_WithTheCorrectIfMatchVersion_Returns200()
    {
        var grant = AdminPermissionGrant.Issue("revoke-target", PermissionCategory.CatalogManagement, "seed-script", DateTimeOffset.UtcNow).Value;
        _factory.GrantRepository.Grants.Add(grant);
        _factory.GrantRepository.Grants.Add(AdminPermissionGrant.Issue("test-admin", PermissionCategory.PermissionManagement, "seed-script", DateTimeOffset.UtcNow).Value);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", grant.Version.ToString());

        var response = await client.PostAsync($"/v1/admin/permission-grants/{grant.GrantId}/revoke", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GrantBody>();
        body!.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokePermissionGrant_WithAStaleIfMatchVersion_Returns409()
    {
        var grant = AdminPermissionGrant.Issue("revoke-target-2", PermissionCategory.CatalogManagement, "seed-script", DateTimeOffset.UtcNow).Value;
        _factory.GrantRepository.Grants.Add(grant);
        _factory.GrantRepository.Grants.Add(AdminPermissionGrant.Issue("test-admin", PermissionCategory.PermissionManagement, "seed-script", DateTimeOffset.UtcNow).Value);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "999");

        var response = await client.PostAsync($"/v1/admin/permission-grants/{grant.GrantId}/revoke", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private sealed record GrantBody(Guid GrantId, string PrincipalId, string Category, DateTimeOffset? RevokedAt, int Version);

    private sealed record PagedBody(int Page, int PageSize, int Total);
}
