using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.DeactivateProduct;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.DeactivateProduct;

public sealed class DeactivateProductCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IProductServiceClient> _client = new();
    private readonly DeactivateProductCommandHandler _handler;

    public DeactivateProductCommandHandlerTests()
    {
        _handler = new DeactivateProductCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_ResolvesTheGroupIdAndDeactivatesTheProduct()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        _client.Setup(c => c.GetProductGroupIdAsync("product-1", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success("group-guid-1"));
        _client.Setup(c => c.DeactivateProductAsync("group-guid-1", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new DeactivateProductCommand(ActingPrincipalId, "product-1", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(ActionNames.ProductDeactivate);
        result.Value.EntityId.Should().Be("product-1");
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ReturnsNotFound()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        _client.Setup(c => c.GetProductGroupIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<string>(Error.NotFound("no such product")));

        var result = await _handler.Handle(new DeactivateProductCommand(ActingPrincipalId, "missing", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
        _client.Verify(c => c.DeactivateProductAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
