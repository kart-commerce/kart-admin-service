using FluentAssertions;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;

namespace KartAdminService.UnitTests.Domain;

public sealed class AdminPermissionGrantTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Issue_WithValidInputs_ProducesALiveGrantAtVersion1()
    {
        var result = AdminPermissionGrant.Issue("principal-1", PermissionCategory.CatalogManagement, "granting-admin", Now);

        result.IsSuccess.Should().BeTrue();
        var grant = result.Value;
        grant.PrincipalId.Should().Be("principal-1");
        grant.Category.Should().Be(PermissionCategory.CatalogManagement);
        grant.GrantedBy.Should().Be("granting-admin");
        grant.IsLive.Should().BeTrue();
        grant.Version.Should().Be(1);
        grant.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void Issue_WithBlankPrincipalId_ReturnsValidationError()
    {
        var result = AdminPermissionGrant.Issue("   ", PermissionCategory.CatalogManagement, "granting-admin", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation_error");
    }

    [Fact]
    public void Revoke_ALiveGrant_SetsRevokedFieldsAndIncrementsVersion()
    {
        var grant = AdminPermissionGrant.Issue("principal-1", PermissionCategory.UserSuspension, "granting-admin", Now).Value;

        var result = grant.Revoke("revoking-admin", Now.AddMinutes(5));

        result.IsSuccess.Should().BeTrue();
        grant.IsLive.Should().BeFalse();
        grant.RevokedBy.Should().Be("revoking-admin");
        grant.RevokedAt.Should().Be(Now.AddMinutes(5));
        grant.Version.Should().Be(2);
    }

    [Fact]
    public void Revoke_AnAlreadyRevokedGrant_ReturnsNotFound()
    {
        var grant = AdminPermissionGrant.Issue("principal-1", PermissionCategory.UserSuspension, "granting-admin", Now).Value;
        grant.Revoke("revoking-admin", Now);

        var result = grant.Revoke("another-admin", Now.AddMinutes(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
        // Revoking twice must not silently succeed or bump the version again - this is the
        // domain-level half of "revocation is the only mutation, and it happens exactly once."
        grant.Version.Should().Be(2);
    }
}
