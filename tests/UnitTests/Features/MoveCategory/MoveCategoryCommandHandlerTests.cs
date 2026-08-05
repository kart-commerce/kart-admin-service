using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.MoveCategory;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.MoveCategory;

public sealed class MoveCategoryCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<ICategoryServiceClient> _client = new();
    private readonly MoveCategoryCommandHandler _handler;

    public MoveCategoryCommandHandlerTests()
    {
        _handler = new MoveCategoryCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WithNullNewParentId_MovesTheCategoryToTopLevel()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        _client.Setup(c => c.MoveCategoryAsync("category-1", null, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new MoveCategoryCommand(ActingPrincipalId, "category-1", null, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(ActionNames.CategoryMove);
    }

    [Fact]
    public async Task Handle_WhenMoveWouldCreateACycle_ReturnsTheCategoryServiceConflict()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        _client
            .Setup(c => c.MoveCategoryAsync("category-1", "descendant-of-category-1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Conflict("would create a cycle")));

        var result = await _handler.Handle(new MoveCategoryCommand(ActingPrincipalId, "category-1", "descendant-of-category-1", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
    }
}
