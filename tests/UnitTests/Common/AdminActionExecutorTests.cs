using FluentAssertions;
using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Domain.Actions;
using KartAdminService.Domain.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KartAdminService.UnitTests.Common;

/// <summary>
/// The shared "authorize -> replay-check -> downstream call -> audit-commit" template 14 of the
/// 16 feature handlers depend on - the single highest-leverage test file in this suite, since a
/// regression here would silently break every one of those handlers' idempotency/authorization
/// guarantees at once.
/// </summary>
public sealed class AdminActionExecutorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly Mock<IPermissionGrantRepository> _grantRepository = new();
    private readonly Mock<IAdminActionRepository> _actionRepository = new();
    private readonly AdminActionExecutor _executor;

    public AdminActionExecutorTests()
    {
        _executor = new AdminActionExecutor(_grantRepository.Object, _actionRepository.Object, TimeProvider.System, NullLogger<AdminActionExecutor>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoLiveGrant_ReturnsPermissionDenied_AndNeverInvokesDownstreamWork()
    {
        _grantRepository.Setup(r => r.HasLiveGrantAsync("admin-1", PermissionCategory.CatalogManagement, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var downstreamInvoked = false;

        var result = await _executor.ExecuteAsync(
            "admin-1", PermissionCategory.CatalogManagement, Guid.NewGuid(), ActionNames.ProductCreate,
            _ => { downstreamInvoked = true; return Task.FromResult(Result.Success("product-1")); },
            context: null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("permission_denied");
        downstreamInvoked.Should().BeFalse();
        _actionRepository.Verify(r => r.AddAndCommitOrGetExistingAsync(It.IsAny<AdminAction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenIdempotencyKeyAlreadyRecorded_ReplaysStoredResult_AndNeverInvokesDownstreamWorkAgain()
    {
        var idempotencyKey = Guid.NewGuid();
        var existing = AdminAction.Record(idempotencyKey, "admin-1", PermissionCategory.CatalogManagement, ActionNames.ProductCreate, "product-1", null, Now).Value;

        _grantRepository.Setup(r => r.HasLiveGrantAsync("admin-1", PermissionCategory.CatalogManagement, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _actionRepository.Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var downstreamInvoked = false;

        var result = await _executor.ExecuteAsync(
            "admin-1", PermissionCategory.CatalogManagement, idempotencyKey, ActionNames.ProductCreate,
            _ => { downstreamInvoked = true; return Task.FromResult(Result.Success("product-1")); },
            context: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActionId.Should().Be(existing.ActionId);
        downstreamInvoked.Should().BeFalse();
        _actionRepository.Verify(r => r.AddAndCommitOrGetExistingAsync(It.IsAny<AdminAction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDownstreamWorkFails_ReturnsThatFailure_AndNeverCommitsAnAuditRow()
    {
        _grantRepository.Setup(r => r.HasLiveGrantAsync("admin-1", PermissionCategory.CatalogManagement, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _actionRepository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((AdminAction?)null);

        var result = await _executor.ExecuteAsync(
            "admin-1", PermissionCategory.CatalogManagement, Guid.NewGuid(), ActionNames.ProductCreate,
            _ => Task.FromResult(Result.Failure<string>(Error.Custom("downstream_unavailable", "Product Service is down."))),
            context: null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("downstream_unavailable");
        _actionRepository.Verify(r => r.AddAndCommitOrGetExistingAsync(It.IsAny<AdminAction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_CommitsAnAuditRowWithTheDownstreamEntityId_AndReturnsIt()
    {
        var idempotencyKey = Guid.NewGuid();
        _grantRepository.Setup(r => r.HasLiveGrantAsync("admin-1", PermissionCategory.CatalogManagement, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _actionRepository.Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>())).ReturnsAsync((AdminAction?)null);
        _actionRepository
            .Setup(r => r.AddAndCommitOrGetExistingAsync(It.IsAny<AdminAction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminAction a, CancellationToken _) => a);

        var result = await _executor.ExecuteAsync(
            "admin-1", PermissionCategory.CatalogManagement, idempotencyKey, ActionNames.ProductCreate,
            _ => Task.FromResult(Result.Success("product-99")),
            context: "{}", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AdminId.Should().Be("admin-1");
        result.Value.EntityId.Should().Be("product-99");
        result.Value.Action.Should().Be(ActionNames.ProductCreate);
        _actionRepository.Verify(
            r => r.AddAndCommitOrGetExistingAsync(It.Is<AdminAction>(a => a.EntityId == "product-99" && a.IdempotencyKey == idempotencyKey), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
