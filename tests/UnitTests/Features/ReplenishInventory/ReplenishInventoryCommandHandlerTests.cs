using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.ReplenishInventory;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.ReplenishInventory;

public sealed class ReplenishInventoryCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IInventoryServiceClient> _client = new();
    private readonly ReplenishInventoryCommandHandler _handler;

    public ReplenishInventoryCommandHandlerTests()
    {
        _handler = new ReplenishInventoryCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_ReplenishesTheSku()
    {
        GrantIsLiveFor(PermissionCategory.InventoryReplenishment);
        _client.Setup(c => c.ReplenishAsync("sku-1", "warehouse-1", 50, "stocktake correction", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var command = new ReplenishInventoryCommand(ActingPrincipalId, "sku-1", "warehouse-1", 50, "stocktake correction", Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityId.Should().Be("sku-1");
        result.Value.Action.Should().Be(ActionNames.InventoryReplenish);
    }

    [Fact]
    public async Task Handle_WhenNotAuthorizedForInventoryReplenishment_ReturnsPermissionDenied()
    {
        GrantIsMissingFor(PermissionCategory.InventoryReplenishment);

        var command = new ReplenishInventoryCommand(ActingPrincipalId, "sku-1", "warehouse-1", 50, null, Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("permission_denied");
    }
}
