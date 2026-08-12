using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.CreateAttribute;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.CreateAttribute;

public sealed class CreateAttributeCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IAttributeServiceClient> _client = new();
    private readonly CreateAttributeCommandHandler _handler;

    public CreateAttributeCommandHandlerTests()
    {
        _handler = new CreateAttributeCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WithLiveGrant_CreatesAttributeAndReturnsEntityId()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        var request = new AttributeWriteRequest("Color", null, "select", [new AttributeValueWriteRequest("Red", 0)]);
        _client.Setup(c => c.CreateAttributeAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success("attribute-1"));

        var result = await _handler.Handle(new CreateAttributeCommand(ActingPrincipalId, request, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityId.Should().Be("attribute-1");
        result.Value.Action.Should().Be(ActionNames.AttributeCreate);
    }

    [Fact]
    public async Task Handle_WithoutLiveGrant_ReturnsPermissionDenied()
    {
        GrantIsMissingFor(PermissionCategory.CatalogManagement);
        var request = new AttributeWriteRequest("Color", null, "select", []);

        var result = await _handler.Handle(new CreateAttributeCommand(ActingPrincipalId, request, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("permission_denied");
        _client.Verify(c => c.CreateAttributeAsync(It.IsAny<AttributeWriteRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
