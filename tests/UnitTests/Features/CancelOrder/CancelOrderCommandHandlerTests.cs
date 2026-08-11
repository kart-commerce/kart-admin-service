using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.CancelOrder;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.CancelOrder;

public sealed class CancelOrderCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IOrderServiceClient> _client = new();
    private readonly CancelOrderCommandHandler _handler;

    public CancelOrderCommandHandlerTests()
    {
        _handler = new CancelOrderCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_CancelsTheOrder()
    {
        var orderId = Guid.NewGuid();
        GrantIsLiveFor(PermissionCategory.OrderManagement);
        _client.Setup(c => c.CancelOrderAsync(orderId, "customer request", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new global::KartAdminService.Application.Features.CancelOrder.CancelOrderCommand(ActingPrincipalId, orderId, "customer request", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(ActionNames.OrderCancel);
        result.Value.EntityId.Should().Be(orderId.ToString());
    }

    [Fact]
    public async Task Handle_WhenNotAuthorized_ReturnsPermissionDenied()
    {
        GrantIsMissingFor(PermissionCategory.OrderManagement);

        var result = await _handler.Handle(new global::KartAdminService.Application.Features.CancelOrder.CancelOrderCommand(ActingPrincipalId, Guid.NewGuid(), null, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("permission_denied");
    }
}
