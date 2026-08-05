using FluentAssertions;
using KartAdminService.Domain.Actions;
using KartAdminService.Domain.Common;

namespace KartAdminService.UnitTests.Domain;

public sealed class AdminActionTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Record_WithValidInputs_ProducesAnUnpublishedRow()
    {
        var idempotencyKey = Guid.NewGuid();

        var result = AdminAction.Record(idempotencyKey, "admin-1", PermissionCategory.CatalogManagement, ActionNames.ProductCreate, "product-42", context: null, Now);

        result.IsSuccess.Should().BeTrue();
        var action = result.Value;
        action.IdempotencyKey.Should().Be(idempotencyKey);
        action.AdminId.Should().Be("admin-1");
        action.Action.Should().Be(ActionNames.ProductCreate);
        action.EntityId.Should().Be("product-42");
        action.PublishedAt.Should().BeNull();
        action.PublishedBy.Should().BeNull();
    }

    [Theory]
    [InlineData("", "product-1")]
    [InlineData("admin-1", "")]
    public void Record_WithBlankRequiredField_ReturnsValidationError(string adminId, string entityId)
    {
        var result = AdminAction.Record(Guid.NewGuid(), adminId, PermissionCategory.CatalogManagement, ActionNames.ProductCreate, entityId, null, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation_error");
    }

    [Fact]
    public void MarkPublished_OnAnUnpublishedRow_SetsPublishedAtAndSystemPollerActor()
    {
        var action = AdminAction.Record(Guid.NewGuid(), "admin-1", PermissionCategory.UserSuspension, ActionNames.UserLock, "user-1", null, Now).Value;

        action.MarkPublished(Now.AddSeconds(5));

        action.PublishedAt.Should().Be(Now.AddSeconds(5));
        action.PublishedBy.Should().Be(AdminAction.OutboxPollerSystemPrincipal);
    }

    [Fact]
    public void MarkPublished_CalledTwice_ThrowsInvalidOperationException()
    {
        var action = AdminAction.Record(Guid.NewGuid(), "admin-1", PermissionCategory.UserSuspension, ActionNames.UserLock, "user-1", null, Now).Value;
        action.MarkPublished(Now);

        var act = () => action.MarkPublished(Now.AddSeconds(1));

        // Mirrors Kart.Shared.Domain.OutboxEventBase's own invariant - a second publish attempt
        // for an already-published row is a bug, never silently accepted.
        act.Should().Throw<InvalidOperationException>();
    }
}
