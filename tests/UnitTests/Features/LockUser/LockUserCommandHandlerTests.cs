using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.LockUser;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.LockUser;

public sealed class LockUserCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IIdentityAdminClient> _client = new();
    private readonly LockUserCommandHandler _handler;

    public LockUserCommandHandlerTests()
    {
        _handler = new LockUserCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_LocksTheUser()
    {
        GrantIsLiveFor(PermissionCategory.UserSuspension);
        _client.Setup(c => c.LockUserAsync("user-1", "fraud review", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new LockUserCommand(ActingPrincipalId, "user-1", "fraud review", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(ActionNames.UserLock);
        result.Value.Context.Should().Contain("fraud review");
    }

    [Fact]
    public async Task Handle_WhenNotAuthorizedForUserSuspension_ReturnsPermissionDenied()
    {
        GrantIsMissingFor(PermissionCategory.UserSuspension);

        var result = await _handler.Handle(new LockUserCommand(ActingPrincipalId, "user-1", null, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("permission_denied");
    }
}
