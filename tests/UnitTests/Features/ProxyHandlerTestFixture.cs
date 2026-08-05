using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Domain.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KartAdminService.UnitTests.Features;

/// <summary>
/// Shared setup for the eleven "proxy" handler test classes (Create/Update/Deactivate Product,
/// Create/Update/Reorder/Move Category, Create/Deactivate Coupon, Lock/Unlock User, Replenish
/// Inventory) - each follows the identical "authorized, no prior idempotent attempt, downstream
/// client returns a result" shape, so the boilerplate lives here once rather than duplicated
/// eleven times (coding-standards.md: three-plus genuinely identical call sites justify
/// extraction).
/// </summary>
public abstract class ProxyHandlerTestFixture
{
    protected const string ActingPrincipalId = "acting-admin";

    protected Mock<IPermissionGrantRepository> GrantRepository { get; } = new();
    protected Mock<IAdminActionRepository> ActionRepository { get; } = new();
    protected AdminActionExecutor Executor { get; }

    protected ProxyHandlerTestFixture()
    {
        Executor = new AdminActionExecutor(GrantRepository.Object, ActionRepository.Object, TimeProvider.System, NullLogger<AdminActionExecutor>.Instance);
        ActionRepository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((AdminAction?)null);
        ActionRepository
            .Setup(r => r.AddAndCommitOrGetExistingAsync(It.IsAny<AdminAction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminAction a, CancellationToken _) => a);
    }

    protected void GrantIsLiveFor(KartAdminService.Domain.Common.PermissionCategory category) =>
        GrantRepository.Setup(r => r.HasLiveGrantAsync(ActingPrincipalId, category, It.IsAny<CancellationToken>())).ReturnsAsync(true);

    protected void GrantIsMissingFor(KartAdminService.Domain.Common.PermissionCategory category) =>
        GrantRepository.Setup(r => r.HasLiveGrantAsync(ActingPrincipalId, category, It.IsAny<CancellationToken>())).ReturnsAsync(false);
}
