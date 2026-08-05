using FluentAssertions;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.ListPermissionGrants;
using KartAdminService.Domain.PermissionGrants;
using Moq;

namespace KartAdminService.UnitTests.Features.ListPermissionGrants;

public sealed class ListPermissionGrantsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAPagedResultMappedFromTheRepository()
    {
        var grant = AdminPermissionGrant.Issue("principal-1", KartAdminService.Domain.Common.PermissionCategory.CatalogManagement, "seed-script", DateTimeOffset.UtcNow).Value;
        var repository = new Mock<IPermissionGrantRepository>();
        repository
            .Setup(r => r.ListAsync(null, null, false, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AdminPermissionGrant> { grant }, 1));

        var handler = new ListPermissionGrantsQueryHandler(repository.Object);
        var result = await handler.Handle(new ListPermissionGrantsQuery(null, null, false, 1, 50), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(1);
        result.Value.Items.Should().ContainSingle().Which.PrincipalId.Should().Be("principal-1");
    }
}
