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
    public async Task Handle_WhenAuthorized_ResolvesTheGroupIdAndForwardsIfMatch_ButKeepsTheKnownSkuAsEntityId()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        var request = new ProductWriteRequest("Widget v2", null, "cat-1", new MoneyDto(12.99m, "USD"), null);
        _client.Setup(c => c.GetProductGroupIdAsync("product-1", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success("group-guid-1"));
        _client.Setup(c => c.UpdateProductAsync("group-guid-1", request, "\"v3\"", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _client.Setup(c => c.UpdatePriceAsync("product-1", request.Price!, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new UpdateProductCommand(ActingPrincipalId, "product-1", request, "\"v3\"", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityId.Should().Be("product-1");
        _client.Verify(c => c.UpdateProductAsync("group-guid-1", request, "\"v3\"", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        // The real bug this proxy exists to fix: the group PATCH alone 200s without ever
        // touching price, so admin-web's price field must reach this separate SKU-keyed call.
        _client.Verify(c => c.UpdatePriceAsync("product-1", request.Price!, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPriceIsUnset_DoesNotCallUpdatePrice()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        var request = new ProductWriteRequest("Widget v2", null, "cat-1", Price: null, null);
        _client.Setup(c => c.GetProductGroupIdAsync("product-1", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success("group-guid-1"));
        _client.Setup(c => c.UpdateProductAsync("group-guid-1", request, "\"v3\"", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new UpdateProductCommand(ActingPrincipalId, "product-1", request, "\"v3\"", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _client.Verify(c => c.UpdatePriceAsync(It.IsAny<string>(), It.IsAny<MoneyDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenThePriceUpdateFails_ReturnsThatFailure()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        var request = new ProductWriteRequest("Widget v2", null, "cat-1", new MoneyDto(12.99m, "USD"), null);
        _client.Setup(c => c.GetProductGroupIdAsync("product-1", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success("group-guid-1"));
        _client.Setup(c => c.UpdateProductAsync("group-guid-1", request, "\"v3\"", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _client
            .Setup(c => c.UpdatePriceAsync("product-1", request.Price!, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Custom("downstream_unavailable", "down")));

        var result = await _handler.Handle(new UpdateProductCommand(ActingPrincipalId, "product-1", request, "\"v3\"", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("downstream_unavailable");
    }

    [Fact]
    public async Task Handle_WhenProductServiceRejectsAStaleVersion_ReturnsConflict()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        var request = new ProductWriteRequest("Widget v2", null, "cat-1", new MoneyDto(12.99m, "USD"), null);
        _client.Setup(c => c.GetProductGroupIdAsync("product-1", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success("group-guid-1"));
        _client
            .Setup(c => c.UpdateProductAsync("group-guid-1", request, "\"stale\"", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Conflict("stale version")));

        var result = await _handler.Handle(new UpdateProductCommand(ActingPrincipalId, "product-1", request, "\"stale\"", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task Handle_WhenTheSkuCannotBeResolvedToAGroupId_ReturnsThatFailure_WithoutCallingUpdate()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        var request = new ProductWriteRequest("Widget v2", null, "cat-1", new MoneyDto(12.99m, "USD"), null);
        _client.Setup(c => c.GetProductGroupIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<string>(Error.NotFound("no such product")));

        var result = await _handler.Handle(new UpdateProductCommand(ActingPrincipalId, "missing", request, "\"v3\"", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
        _client.Verify(c => c.UpdateProductAsync(It.IsAny<string>(), It.IsAny<ProductWriteRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
