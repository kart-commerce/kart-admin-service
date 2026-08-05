using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;

namespace KartAdminService.ContractTests;

/// <summary>api-contract.yaml /admin/coupons* (ADM-11, ADM-12).</summary>
public sealed class CouponsContractTests : IClassFixture<AdminContractTestFactory>
{
    private readonly AdminContractTestFactory _factory;

    public CouponsContractTests(AdminContractTestFactory factory)
    {
        _factory = factory;
        _factory.GrantRepository.Grants.Add(AdminPermissionGrant.Issue("test-admin", PermissionCategory.CouponIssuance, "seed-script", DateTimeOffset.UtcNow).Value);
    }

    [Fact]
    public async Task PostCoupons_AsAuthorizedAdmin_Returns201()
    {
        _factory.OfferClient.CreateResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/coupons", new { couponCode = "SAVE10", discountValue = new { amount = 10, currency = "USD" } });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ActionBody>();
        body!.EntityId.Should().Be("SAVE10");
    }

    [Fact]
    public async Task PostCoupons_WhenCouponCodeAlreadyExists_Returns409()
    {
        _factory.OfferClient.CreateResult = Result.Failure(Error.Conflict("already exists"));
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/v1/admin/coupons", new { couponCode = "SAVE10", discountValue = new { amount = 10, currency = "USD" } });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeactivateCoupon_Returns200()
    {
        _factory.OfferClient.DeactivateResult = Result.Success();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, "admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsync("/v1/admin/coupons/SAVE10/deactivate", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record ActionBody(Guid ActionId, string EntityId);
}
