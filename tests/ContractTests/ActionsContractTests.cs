using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using KartAdminService.Domain.Actions;
using KartAdminService.Domain.Common;

namespace KartAdminService.ContractTests;

/// <summary>
/// api-contract.yaml GET /admin/actions (ADM-16). Deliberately coarser than the write path -
/// any caller with the coarse Admin claim can read it, no fine-grained category grant needed.
/// </summary>
public sealed class ActionsContractTests : IClassFixture<AdminContractTestFactory>
{
    private readonly AdminContractTestFactory _factory;

    public ActionsContractTests(AdminContractTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetActions_AsAnyAdmin_Returns200_WithNoFineGrainedGrantRequired()
    {
        _factory.ActionRepository.Actions.Add(
            AdminAction.Record(Guid.NewGuid(), "some-admin", PermissionCategory.UserSuspension, ActionNames.UserLock, "user-1", null, DateTimeOffset.UtcNow).Value);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("X-Test-Sub", "reader-with-zero-category-grants");

        var response = await client.GetAsync("/v1/admin/actions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedBody>();
        body!.Total.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetActions_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/v1/admin/actions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetActions_WithoutAdminRole_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "customer");

        var response = await client.GetAsync("/v1/admin/actions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record PagedBody(int Total);
}
