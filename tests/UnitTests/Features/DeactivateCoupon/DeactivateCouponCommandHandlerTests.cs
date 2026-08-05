using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.DeactivateCoupon;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.DeactivateCoupon;

public sealed class DeactivateCouponCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IOfferServiceClient> _client = new();
    private readonly DeactivateCouponCommandHandler _handler;

    public DeactivateCouponCommandHandlerTests()
    {
        _handler = new DeactivateCouponCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_DeactivatesTheCoupon()
    {
        GrantIsLiveFor(PermissionCategory.CouponIssuance);
        _client.Setup(c => c.DeactivateCouponAsync("SAVE10", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new DeactivateCouponCommand(ActingPrincipalId, "SAVE10", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(ActionNames.CouponDeactivate);
    }
}
