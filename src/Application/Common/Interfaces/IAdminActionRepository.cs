using KartAdminService.Domain.Actions;
using KartAdminService.Domain.Common;

namespace KartAdminService.Application.Common.Interfaces;

/// <summary>Persistence abstraction for the AdminAction aggregate (audit trail + Outbox row).</summary>
public interface IAdminActionRepository
{
    /// <summary>
    /// The idempotency-replay lookup every mutating handler runs before doing any downstream
    /// work (design-decisions.md, "Idempotency Mechanism for Outbound Write Calls"). A non-null
    /// result means this exact admin-action attempt already completed — the caller returns it
    /// directly instead of re-executing.
    /// </summary>
    Task<AdminAction?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts and commits <paramref name="action"/>. If a concurrent request already committed
    /// a row for the same IdempotencyKey between the caller's own replay check and this call —
    /// a true race, not a sequential replay — the DB's unique index on idempotency_key is the
    /// actual safety net: this method catches that violation internally (Infrastructure owns the
    /// Npgsql-specific exception shape, never Application) and returns the row the *other*,
    /// faster caller committed instead of throwing or producing a duplicate.
    /// </summary>
    Task<AdminAction> AddAndCommitOrGetExistingAsync(AdminAction action, CancellationToken cancellationToken);

    /// <summary>Paginated, optionally filtered — api-contract.yaml GET /admin/actions. Deliberately not category-scoped by the caller's own grants (requirement-spec.md §4: audit reads are coarse).</summary>
    Task<(IReadOnlyList<AdminAction> Items, int Total)> ListAsync(
        string? adminId,
        PermissionCategory? category,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
