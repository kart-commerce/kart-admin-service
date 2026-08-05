using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.ReorderCategory;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.ReorderCategory;

public sealed class ReorderCategoryCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<ICategoryServiceClient> _client = new();
    private readonly ReorderCategoryCommandHandler _handler;

    public ReorderCategoryCommandHandlerTests()
    {
        _handler = new ReorderCategoryCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_ReordersTheCategory()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        _client.Setup(c => c.ReorderCategoryAsync("category-1", 3, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new ReorderCategoryCommand(ActingPrincipalId, "category-1", 3, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(ActionNames.CategoryReorder);
    }
}
