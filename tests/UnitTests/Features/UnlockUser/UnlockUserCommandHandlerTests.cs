using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.UnlockUser;
using KartAdminService.Domain.Common;
using KartAdminService.UnitTests.Features;
using Moq;

namespace KartAdminService.UnitTests.Features.UnlockUser;

public sealed class UnlockUserCommandHandlerTests : ProxyHandlerTestFixture
{
    private readonly Mock<IIdentityAdminClient> _client = new();
    private readonly UnlockUserCommandHandler _handler;

    public UnlockUserCommandHandlerTests()
    {
        _handler = new UnlockUserCommandHandler(Executor, _client.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_UnlocksTheUser()
    {
        GrantIsLiveFor(PermissionCategory.UserSuspension);
        _client.Setup(c => c.UnlockUserAsync("user-1", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new UnlockUserCommand(ActingPrincipalId, "user-1", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(ActionNames.UserUnlock);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
    {
        GrantIsLiveFor(PermissionCategory.UserSuspension);
        _client.Setup(c => c.UnlockUserAsync("missing", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure(Error.NotFound("no such user")));

        var result = await _handler.Handle(new UnlockUserCommand(ActingPrincipalId, "missing", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
    }
}
