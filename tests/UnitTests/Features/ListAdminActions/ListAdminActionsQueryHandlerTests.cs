using FluentAssertions;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.ListAdminActions;
using KartAdminService.Domain.Actions;
using KartAdminService.Domain.Common;
using Moq;

namespace KartAdminService.UnitTests.Features.ListAdminActions;

public sealed class ListAdminActionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAPagedResultMappedFromTheRepository()
    {
        var action = AdminAction.Record(Guid.NewGuid(), "admin-1", PermissionCategory.UserSuspension, ActionNames.UserLock, "user-1", null, DateTimeOffset.UtcNow).Value;
        var repository = new Mock<IAdminActionRepository>();
        repository
            .Setup(r => r.ListAsync(null, null, null, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AdminAction> { action }, 1));

        var handler = new ListAdminActionsQueryHandler(repository.Object);
        var result = await handler.Handle(new ListAdminActionsQuery(null, null, null, null, 1, 50), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(1);
        result.Value.Items.Should().ContainSingle().Which.EntityId.Should().Be("user-1");
    }
}
