using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.DeprecateAttribute;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.DeprecateAttribute;

public sealed class DeprecateAttributeCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IAttributeServiceClient> _client = new();
    private readonly DeprecateAttributeCommandHandler _handler;

    public DeprecateAttributeCommandHandlerTests()
    {
        _handler = new DeprecateAttributeCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WithLiveGrant_DeprecatesAndReturnsKnownEntityId()
    {
        GrantIsLiveFor(PermissionCategory.CatalogManagement);
        _client.Setup(c => c.DeprecateAttributeAsync("attribute-1", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new DeprecateAttributeCommand(ActingPrincipalId, "attribute-1", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityId.Should().Be("attribute-1");
        result.Value.Action.Should().Be(ActionNames.AttributeDeprecate);
    }

    [Fact]
    public async Task Handle_WithoutLiveGrant_ReturnsPermissionDenied()
    {
        GrantIsMissingFor(PermissionCategory.CatalogManagement);

        var result = await _handler.Handle(new DeprecateAttributeCommand(ActingPrincipalId, "attribute-1", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("permission_denied");
        _client.Verify(c => c.DeprecateAttributeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
