using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.RevokePermissionGrant;
using KartAdminService.Domain.Actions;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KartAdminService.UnitTests.Features.RevokePermissionGrant;

public sealed class RevokePermissionGrantCommandHandlerTests
{
    private readonly Mock<IPermissionGrantRepository> _grantRepository = new();
    private readonly Mock<IAdminActionRepository> _actionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RevokePermissionGrantCommandHandler _handler;

    public RevokePermissionGrantCommandHandlerTests()
    {
        var executor = new AdminActionExecutor(_grantRepository.Object, _actionRepository.Object, TimeProvider.System, NullLogger<AdminActionExecutor>.Instance);
        _handler = new RevokePermissionGrantCommandHandler(executor, _grantRepository.Object, _unitOfWork.Object, TimeProvider.System);

        _grantRepository.Setup(r => r.HasLiveGrantAsync("acting-admin", PermissionCategory.PermissionManagement, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _actionRepository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((AdminAction?)null);
        _actionRepository
            .Setup(r => r.AddAndCommitOrGetExistingAsync(It.IsAny<AdminAction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminAction a, CancellationToken _) => a);
    }

    [Fact]
    public async Task Handle_WhenGrantDoesNotExist_ReturnsNotFound()
    {
        _grantRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((AdminPermissionGrant?)null);

        var command = new RevokePermissionGrantCommand("acting-admin", Guid.NewGuid(), 1, Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task Handle_WhenExpectedVersionIsStale_ReturnsConflict_AndNeverSaves()
    {
        var grant = AdminPermissionGrant.Issue("target", PermissionCategory.CatalogManagement, "seed-script", DateTimeOffset.UtcNow).Value;
        _grantRepository.Setup(r => r.GetByIdAsync(grant.GrantId, It.IsAny<CancellationToken>())).ReturnsAsync(grant);

        var command = new RevokePermissionGrantCommand("acting-admin", grant.GrantId, ExpectedVersion: 99, Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTheCorrectExpectedVersion_RevokesTheGrant()
    {
        var grant = AdminPermissionGrant.Issue("target", PermissionCategory.CatalogManagement, "seed-script", DateTimeOffset.UtcNow).Value;
        _grantRepository.Setup(r => r.GetByIdAsync(grant.GrantId, It.IsAny<CancellationToken>())).ReturnsAsync(grant);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var command = new RevokePermissionGrantCommand("acting-admin", grant.GrantId, ExpectedVersion: 1, Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RevokedAt.Should().NotBeNull();
        result.Value.RevokedBy.Should().Be("acting-admin");
        grant.IsLive.Should().BeFalse();
    }
}
