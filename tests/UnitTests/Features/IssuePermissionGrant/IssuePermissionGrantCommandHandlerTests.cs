using FluentAssertions;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Features.IssuePermissionGrant;
using KartAdminService.Domain.Actions;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KartAdminService.UnitTests.Features.IssuePermissionGrant;

public sealed class IssuePermissionGrantCommandHandlerTests
{
    private readonly Mock<IPermissionGrantRepository> _grantRepository = new();
    private readonly Mock<IAdminActionRepository> _actionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly IssuePermissionGrantCommandHandler _handler;

    public IssuePermissionGrantCommandHandlerTests()
    {
        var executor = new AdminActionExecutor(_grantRepository.Object, _actionRepository.Object, TimeProvider.System, NullLogger<AdminActionExecutor>.Instance);
        _handler = new IssuePermissionGrantCommandHandler(executor, _grantRepository.Object, _unitOfWork.Object, TimeProvider.System);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Kart.Shared.Domain.Result.Success());
        _actionRepository
            .Setup(r => r.AddAndCommitOrGetExistingAsync(It.IsAny<AdminAction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminAction a, CancellationToken _) => a);
    }

    [Fact]
    public async Task Handle_WhenActorHasNoLivePermissionManagementGrant_ReturnsPermissionDenied()
    {
        _grantRepository.Setup(r => r.HasLiveGrantAsync("acting-admin", PermissionCategory.PermissionManagement, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var command = new IssuePermissionGrantCommand("acting-admin", "target-principal", PermissionCategory.CatalogManagement, Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("permission_denied");
        _grantRepository.Verify(r => r.AddAsync(It.IsAny<AdminPermissionGrant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTargetAlreadyHasALiveGrantForThatCategory_ReturnsConflict()
    {
        _grantRepository.Setup(r => r.HasLiveGrantAsync("acting-admin", PermissionCategory.PermissionManagement, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _grantRepository.Setup(r => r.HasLiveGrantAsync("target-principal", PermissionCategory.CatalogManagement, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new IssuePermissionGrantCommand("acting-admin", "target-principal", PermissionCategory.CatalogManagement, Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task Handle_WhenAuthorized_IssuesTheGrantAndReturnsIt()
    {
        _grantRepository.Setup(r => r.HasLiveGrantAsync("acting-admin", PermissionCategory.PermissionManagement, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _grantRepository.Setup(r => r.HasLiveGrantAsync("target-principal", PermissionCategory.CatalogManagement, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        AdminPermissionGrant? added = null;
        _grantRepository
            .Setup(r => r.AddAsync(It.IsAny<AdminPermissionGrant>(), It.IsAny<CancellationToken>()))
            .Callback<AdminPermissionGrant, CancellationToken>((g, _) => added = g)
            .Returns(Task.CompletedTask);
        _grantRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => added);

        var command = new IssuePermissionGrantCommand("acting-admin", "target-principal", PermissionCategory.CatalogManagement, Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PrincipalId.Should().Be("target-principal");
        result.Value.Category.Should().Be("catalog-management");
        result.Value.GrantedBy.Should().Be("acting-admin");
    }
}
