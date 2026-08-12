using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.AdminUpdateOrderStatus;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.AdminUpdateOrderStatus;

public sealed class AdminUpdateOrderStatusCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IOrderServiceClient> _client = new();
    private readonly AdminUpdateOrderStatusCommandHandler _handler;

    public AdminUpdateOrderStatusCommandHandlerTests()
    {
        _handler = new AdminUpdateOrderStatusCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_UpdatesTheStatus()
    {
        var orderId = Guid.NewGuid();
        GrantIsLiveFor(PermissionCategory.OrderManagement);
        _client.Setup(c => c.UpdateStatusAsync(orderId, "Shipped", "courier confirmed out-of-band", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new AdminUpdateOrderStatusCommand(ActingPrincipalId, orderId, "Shipped", "courier confirmed out-of-band", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(ActionNames.OrderStatusUpdate);
    }

    [Fact]
    public async Task Handle_WhenOrderServiceRejectsTheTransition_ReturnsTheDownstreamFailure()
    {
        var orderId = Guid.NewGuid();
        GrantIsLiveFor(PermissionCategory.OrderManagement);
        _client.Setup(c => c.UpdateStatusAsync(orderId, "Delivered", "reason", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Conflict("Order cannot transition to 'Delivered' from status 'Created'.")));

        var result = await _handler.Handle(new AdminUpdateOrderStatusCommand(ActingPrincipalId, orderId, "Delivered", "reason", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
    }
}
