using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.CreateCoupon;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.CreateCoupon;

public sealed class CreateCouponCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IOfferServiceClient> _client = new();
    private readonly CreateCouponCommandHandler _handler;

    public CreateCouponCommandHandlerTests()
    {
        _handler = new CreateCouponCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_CreatesTheCoupon_UsingItsOwnCouponCodeAsEntityId()
    {
        GrantIsLiveFor(PermissionCategory.CouponIssuance);
        var request = new CouponWriteRequest("SAVE10", new MoneyDto(10, "USD"), null, null, null, null);
        _client.Setup(c => c.CreateCouponAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new CreateCouponCommand(ActingPrincipalId, request, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityId.Should().Be("SAVE10");
    }

    [Fact]
    public async Task Handle_WhenCouponCodeAlreadyExists_ReturnsConflict()
    {
        GrantIsLiveFor(PermissionCategory.CouponIssuance);
        var request = new CouponWriteRequest("SAVE10", new MoneyDto(10, "USD"), null, null, null, null);
        _client.Setup(c => c.CreateCouponAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure(Error.Conflict("already exists")));

        var result = await _handler.Handle(new CreateCouponCommand(ActingPrincipalId, request, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
    }
}
