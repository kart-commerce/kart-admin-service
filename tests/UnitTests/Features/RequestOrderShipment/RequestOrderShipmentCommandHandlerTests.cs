using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.RequestOrderShipment;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.RequestOrderShipment;

public sealed class RequestOrderShipmentCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IOrderServiceClient> _client = new();
    private readonly RequestOrderShipmentCommandHandler _handler;

    public RequestOrderShipmentCommandHandlerTests()
    {
        _handler = new RequestOrderShipmentCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_RequestsShipment()
    {
        var orderId = Guid.NewGuid();
        GrantIsLiveFor(PermissionCategory.OrderManagement);
        _client.Setup(c => c.RequestShipmentAsync(orderId, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new RequestOrderShipmentCommand(ActingPrincipalId, orderId, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(ActionNames.OrderShipmentRequest);
    }

    [Fact]
    public async Task Handle_WhenOrderIsNotPaid_ReturnsTheDownstreamConflict()
    {
        var orderId = Guid.NewGuid();
        GrantIsLiveFor(PermissionCategory.OrderManagement);
        _client.Setup(c => c.RequestShipmentAsync(orderId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Conflict("Order must be 'Paid' to request shipment.")));

        var result = await _handler.Handle(new RequestOrderShipmentCommand(ActingPrincipalId, orderId, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
    }
}
