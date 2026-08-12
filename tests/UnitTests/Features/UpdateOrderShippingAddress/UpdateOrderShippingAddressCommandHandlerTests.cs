using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.UpdateOrderShippingAddress;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.UpdateOrderShippingAddress;

public sealed class UpdateOrderShippingAddressCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IOrderServiceClient> _client = new();
    private readonly UpdateOrderShippingAddressCommandHandler _handler;

    private static readonly ShippingAddressWriteRequest Address = new(
        "Jane Doe", "1 Test St", null, "Testville", "TS", "00000", "US", null);

    public UpdateOrderShippingAddressCommandHandlerTests()
    {
        _handler = new UpdateOrderShippingAddressCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_UpdatesTheAddress()
    {
        var orderId = Guid.NewGuid();
        GrantIsLiveFor(PermissionCategory.OrderManagement);
        _client.Setup(c => c.UpdateShippingAddressAsync(orderId, Address, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new UpdateOrderShippingAddressCommand(ActingPrincipalId, orderId, Address, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(ActionNames.OrderShippingAddressUpdate);
    }
}
