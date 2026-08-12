using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.UpdateAttribute;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.UpdateAttribute;

public sealed class UpdateAttributeCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IAttributeServiceClient> _client = new();
    private readonly UpdateAttributeCommandHandler _handler;

    public UpdateAttributeCommandHandlerTests()
    {
        _handler = new UpdateAttributeCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WithLiveGrant_UpdatesAndReturnsKnownEntityId()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        var request = new AttributeUpdateRequest("Primary Color", [new AttributeValueWriteRequest("Red", 0)]);
        _client.Setup(c => c.UpdateAttributeAsync("attribute-1", request, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new UpdateAttributeCommand(ActingPrincipalId, "attribute-1", request, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityId.Should().Be("attribute-1");
        result.Value.Action.Should().Be(ActionNames.AttributeUpdate);
    }

    [Fact]
    public async Task Handle_WhenCategoryServiceReturnsNotFound_PropagatesTheError()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        var request = new AttributeUpdateRequest("Primary Color", []);
        _client
            .Setup(c => c.UpdateAttributeAsync("attribute-1", request, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.NotFound("not found")));

        var result = await _handler.Handle(new UpdateAttributeCommand(ActingPrincipalId, "attribute-1", request, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
    }
}
