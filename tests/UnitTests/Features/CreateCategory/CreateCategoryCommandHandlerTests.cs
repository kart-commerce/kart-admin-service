using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.CreateCategory;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.CreateCategory;

public sealed class CreateCategoryCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<ICategoryServiceClient> _client = new();
    private readonly CreateCategoryCommandHandler _handler;

    public CreateCategoryCommandHandlerTests()
    {
        _handler = new CreateCategoryCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_CreatesTheCategory()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        var request = new CategoryWriteRequest("Electronics", null, null);
        _client.Setup(c => c.CreateCategoryAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success("category-1"));

        var result = await _handler.Handle(new CreateCategoryCommand(ActingPrincipalId, request, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityId.Should().Be("category-1");
        result.Value.Action.Should().Be(ActionNames.CategoryCreate);
    }

    [Fact]
    public async Task Handle_WhenNotAuthorized_ReturnsPermissionDenied()
    {
        GrantIsMissingFor(PermissionCategory.CatalogManagement);
        var request = new CategoryWriteRequest("Electronics", null, null);

        var result = await _handler.Handle(new CreateCategoryCommand(ActingPrincipalId, request, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("permission_denied");
    }
}
