using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.UpdateProduct;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.UpdateProduct;

public sealed class UpdateProductCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IProductServiceClient> _client = new();
    private readonly UpdateProductCommandHandler _handler;

    public UpdateProductCommandHandlerTests()
    {
        _handler = new UpdateProductCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_ForwardsIfMatchToProductService_AndKeepsTheKnownProductIdAsEntityId()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        var request = new ProductWriteRequest("Widget v2", null, "cat-1", new MoneyDto(12.99m, "USD"), null);
        _client.Setup(c => c.UpdateProductAsync("product-1", request, "\"v3\"", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new UpdateProductCommand(ActingPrincipalId, "product-1", request, "\"v3\"", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityId.Should().Be("product-1");
        _client.Verify(c => c.UpdateProductAsync("product-1", request, "\"v3\"", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProductServiceRejectsAStaleVersion_ReturnsConflict()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        var request = new ProductWriteRequest("Widget v2", null, "cat-1", new MoneyDto(12.99m, "USD"), null);
        _client
            .Setup(c => c.UpdateProductAsync("product-1", request, "\"stale\"", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Conflict("stale version")));

        var result = await _handler.Handle(new UpdateProductCommand(ActingPrincipalId, "product-1", request, "\"stale\"", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
    }
}
