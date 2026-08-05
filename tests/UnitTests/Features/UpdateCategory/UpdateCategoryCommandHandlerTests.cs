using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.UpdateCategory;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.UpdateCategory;

public sealed class UpdateCategoryCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<ICategoryServiceClient> _client = new();
    private readonly UpdateCategoryCommandHandler _handler;

    public UpdateCategoryCommandHandlerTests()
    {
        _handler = new UpdateCategoryCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_UpdatesTheCategory()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        var request = new CategoryWriteRequest("Electronics v2", null, null);
        _client.Setup(c => c.UpdateCategoryAsync("category-1", request, "\"v2\"", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new UpdateCategoryCommand(ActingPrincipalId, "category-1", request, "\"v2\"", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityId.Should().Be("category-1");
    }
}
