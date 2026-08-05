using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;

namespace KartAdminService.ContractTests;

/// <summary>api-contract.yaml /admin/users/{userId}/lock|unlock (ADM-13, ADM-14).</summary>
public sealed class UsersContractTests : IClassFixture<AdminContractTestFactory>
{
    private readonly AdminContractTestFactory _factory;

    public UsersContractTests(AdminContractTestFactory factory)
    {
        _factory = factory;
        _factory.GrantRepository.Grants.Add(AdminPermissionGrant.Issue("test-admin", PermissionCategory.UserSuspension, "seed-script", DateTimeOffset.UtcNow).Value);
    }

    [Fact]
    public async Task LockUser_AsAuthorizedAdmin_Returns200()
    {
        _factory.IdentityClient.LockResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/users/user-1/lock", new { reason = "fraud review" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ActionBody>();
        body!.EntityId.Should().Be("user-1");
    }

    [Fact]
    public async Task LockUser_WhenUserDoesNotExist_Returns404()
    {
        _factory.IdentityClient.LockResult = Result.Failure(Error.NotFound("no such user"));
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/users/missing/lock", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnlockUser_Returns200()
    {
        _factory.IdentityClient.UnlockResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsync("/v1/admin/users/user-1/unlock", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LockUser_WithoutUserSuspensionGrant_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("X-Test-Sub", "unprivileged");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/users/user-1/lock", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record ActionBody(Guid ActionId, string EntityId);
}
