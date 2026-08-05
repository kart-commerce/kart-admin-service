using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.CreateProduct;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.CreateProduct;

public sealed class CreateProductCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IProductServiceClient> _client = new();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _handler = new CreateProductCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenNotAuthorizedForCatalogManagement_ReturnsPermissionDenied_AndNeverCallsProductService()
    {
        GrantIsMissingFor(PermissionCategory.CatalogManagement);
        var request = new ProductWriteRequest("Widget", null, "cat-1", new MoneyDto(9.99m, "USD"), null);

        var result = await _handler.Handle(new CreateProductCommand(ActingPrincipalId, request, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("permission_denied");
        _client.Verify(c => c.CreateProductAsync(It.IsAny<ProductWriteRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_CallsProductServiceAndRecordsTheAction()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        var request = new ProductWriteRequest("Widget", null, "cat-1", new MoneyDto(9.99m, "USD"), null);
        _client.Setup(c => c.CreateProductAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success("product-99"));

        var result = await _handler.Handle(new CreateProductCommand(ActingPrincipalId, request, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityId.Should().Be("product-99");
        result.Value.Action.Should().Be(ActionNames.ProductCreate);
    }

    [Fact]
    public async Task Handle_WhenProductServiceIsUnavailable_ReturnsDownstreamUnavailable()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        var request = new ProductWriteRequest("Widget", null, "cat-1", new MoneyDto(9.99m, "USD"), null);
        _client
            .Setup(c => c.CreateProductAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>(Error.Custom("downstream_unavailable", "Product Service is unavailable.")));

        var result = await _handler.Handle(new CreateProductCommand(ActingPrincipalId, request, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("downstream_unavailable");
    }
}
