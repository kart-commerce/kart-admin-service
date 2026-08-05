using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Actions;
using KartAdminService.Domain.Common;
using Microsoft.Extensions.Logging;

namespace KartAdminService.Application.Common;

/// <summary>
/// The shared "authorize → replay-check → downstream call → audit-commit" template every
/// mutating /admin/* handler follows (requirement-spec.md Domain Invariant #1/#2;
/// design-decisions.md's Caching/Idempotency/Concurrency decisions). Extracted here once 14 of
/// the 16 feature handlers turned out to need the identical shape (coding-standards.md: "three
/// concrete, similar call sites justify extracting an abstraction") rather than duplicating it
/// per vertical slice, which would risk one slice's copy silently drifting from another's on the
/// idempotency/authorization guarantees this service exists to provide.
///
/// Steps, in order:
/// 1. Fine-grained authorization — a live grant for (principalId, category), read fresh/uncached
///    (design-decisions.md, "Caching Strategy for Fine-Grained Permission Grants"). Absence →
///    403 `permission_denied`, even with a valid coarse Admin claim.
/// 2. Idempotency replay — an existing admin_actions row for this IdempotencyKey means this
///    exact attempt already completed; its stored result is returned directly and
///    <paramref name="performDownstreamWork"/> is never invoked again. This is the concrete
///    "no double coupon issuance / no double user-lock / no double payment-shaped action"
///    guarantee.
/// 3. The caller-supplied downstream work (a synchronous owning-service call, or a purely local
///    computation for permission-management actions) runs. Its failure is returned as-is — no
///    local admin_actions row is written for a downstream failure (Domain Invariant #2's
///    ordering: the row is only ever a true fact).
/// 4. On success, the audit/outbox row is inserted and committed. A concurrent duplicate insert
///    for the same IdempotencyKey (a true race, not a sequential replay) is caught via the
///    unique-index violation and resolved by re-fetching the row the other, faster caller just
///    committed — never a duplicate row, never an error surfaced to either caller.
/// </summary>
public sealed class AdminActionExecutor
{
    private readonly IPermissionGrantRepository _grantRepository;
    private readonly IAdminActionRepository _actionRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AdminActionExecutor> _logger;

    public AdminActionExecutor(
        IPermissionGrantRepository grantRepository,
        IAdminActionRepository actionRepository,
        TimeProvider timeProvider,
        ILogger<AdminActionExecutor> logger)
    {
        _grantRepository = grantRepository;
        _actionRepository = actionRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<AdminActionResultDto>> ExecuteAsync(
        string principalId,
        PermissionCategory category,
        Guid idempotencyKey,
        string actionName,
        Func<CancellationToken, Task<Result<string>>> performDownstreamWork,
        string? context,
        CancellationToken cancellationToken)
    {
        var hasLiveGrant = await _grantRepository.HasLiveGrantAsync(principalId, category, cancellationToken);
        if (!hasLiveGrant)
        {
            return Result.Failure<AdminActionResultDto>(
                Error.Custom("permission_denied", $"Principal '{principalId}' has no live grant for category '{category.ToWireValue()}'."));
        }

        var existing = await _actionRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation(
                "Idempotency-Key {IdempotencyKey} already recorded as {ActionId} ({Action}); replaying stored result.",
                idempotencyKey,
                existing.ActionId,
                existing.Action);
            return Result.Success(AdminActionResultDto.FromDomain(existing));
        }

        var downstreamResult = await performDownstreamWork(cancellationToken);
        if (downstreamResult.IsFailure)
        {
            return Result.Failure<AdminActionResultDto>(downstreamResult.Error);
        }

        var now = _timeProvider.GetUtcNow();
        var recordResult = AdminAction.Record(idempotencyKey, principalId, category, actionName, downstreamResult.Value, context, now);
        if (recordResult.IsFailure)
        {
            return Result.Failure<AdminActionResultDto>(recordResult.Error);
        }

        var action = recordResult.Value;

        // AddAndCommitOrGetExistingAsync owns the concurrent-duplicate-insert race internally
        // (Infrastructure catches the DB's unique-index violation and re-fetches the winning
        // row) — Application never needs to know the write provider's exception shape.
        var committed = await _actionRepository.AddAndCommitOrGetExistingAsync(action, cancellationToken);
        return Result.Success(AdminActionResultDto.FromDomain(committed));
    }
}
