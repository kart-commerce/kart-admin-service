using Kart.Shared.Domain;
using KartAdminService.Domain.Common;

namespace KartAdminService.Domain.Actions;

/// <summary>
/// Admin's append-only audit trail AND its Outbox row for AdminActionPerformed, in one table
/// (database-design.md admin_actions; ddd-model.md's AdminAction aggregate). A row is written
/// exactly once, in the same local transaction as the completed back-office action, only after
/// the synchronous outbound call to the owning service has already succeeded (Domain Invariant
/// #2) — that ordering is enforced by the application handler, not this entity. Append-only at
/// the application layer: no setter mutates AdminId/Category/Action/EntityId/Context/PerformedAt
/// after construction; the sole exception is <see cref="MarkPublished"/>.
///
/// This does not inherit Kart.Shared.Domain.OutboxEventBase: that base models a generic
/// EventType/Payload blob shape, but the already-approved database-design.md schema instead
/// has bespoke structured columns (Category/Action/EntityId/Context/IdempotencyKey) — the outbox
/// relay computes the AdminActionPerformed JSON payload ({adminId, action, entityId}) from these
/// columns at publish time rather than storing a precomputed blob. The MarkPublished
/// "throws if already published" invariant is still preserved, matching OutboxEventBase's own
/// invariant.
/// </summary>
public sealed class AdminAction
{
    public const string OutboxPollerSystemPrincipal = "system:admin-outbox-poller";

    public Guid ActionId { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public string AdminId { get; private set; } = string.Empty;
    public PermissionCategory Category { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string? Context { get; private set; }
    public DateTimeOffset PerformedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public string? PublishedBy { get; private set; }

    /// <summary>EF Core materialization only.</summary>
    private AdminAction()
    {
    }

    private AdminAction(
        Guid actionId,
        Guid idempotencyKey,
        string adminId,
        PermissionCategory category,
        string action,
        string entityId,
        string? context,
        DateTimeOffset now)
    {
        ActionId = actionId;
        IdempotencyKey = idempotencyKey;
        AdminId = adminId;
        Category = category;
        Action = action;
        EntityId = entityId;
        Context = context;
        PerformedAt = now;
    }

    /// <summary>
    /// Records a completed admin action. Only ever called after Domain Invariant #2's ordering
    /// has already been satisfied by the caller (the downstream owning-service call, if any, has
    /// already succeeded) — this factory does not itself call anything downstream.
    /// </summary>
    public static Result<AdminAction> Record(
        Guid idempotencyKey,
        string adminId,
        PermissionCategory category,
        string action,
        string entityId,
        string? context,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(adminId))
        {
            return Result.Failure<AdminAction>(Error.Validation("adminId is required."));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            return Result.Failure<AdminAction>(Error.Validation("action is required."));
        }

        if (string.IsNullOrWhiteSpace(entityId))
        {
            return Result.Failure<AdminAction>(Error.Validation("entityId is required."));
        }

        return Result.Success(new AdminAction(Guid.NewGuid(), idempotencyKey, adminId.Trim(), category, action.Trim(), entityId.Trim(), context, now));
    }

    /// <summary>
    /// Marks this row as relayed by the Outbox poller — the one field exempt from this
    /// aggregate's append-only rule. Throws if already published, mirroring
    /// Kart.Shared.Domain.OutboxEventBase.MarkPublished's own invariant.
    /// </summary>
    public void MarkPublished(DateTimeOffset publishedAt)
    {
        if (PublishedAt is not null)
        {
            throw new InvalidOperationException(
                $"Admin action {ActionId} was already published at {PublishedAt:O} and cannot be re-published.");
        }

        PublishedAt = publishedAt;
        PublishedBy = OutboxPollerSystemPrincipal;
    }
}
