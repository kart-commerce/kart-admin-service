using Kart.Shared.Domain;
using KartAdminService.Domain.Common;

namespace KartAdminService.Domain.PermissionGrants;

/// <summary>
/// The fine-grained, category-scoped permission-grant ledger (database-design.md
/// admin_permission_grants; ddd-model.md's AdminPermissionGrant aggregate). At most one *live*
/// (non-revoked) grant per (PrincipalId, Category) — enforced here defensively and, as the
/// single source of truth, by the DB's partial unique index uq_admin_permission_grants_live.
/// No AggregateRoot/IDomainEvent base is used (ddd-model.md Modeling Decision #3: this aggregate
/// raises no domain events of its own — issuing/revoking a grant is recorded by a paired
/// AdminAction row instead, written by the application handler, not raised from here).
/// </summary>
public sealed class AdminPermissionGrant
{
    public const string SeedScriptPrincipal = "seed-script";

    public Guid GrantId { get; private set; }
    public string PrincipalId { get; private set; } = string.Empty;
    public PermissionCategory Category { get; private set; }
    public DateTimeOffset GrantedAt { get; private set; }
    public string GrantedBy { get; private set; } = string.Empty;
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedBy { get; private set; }
    public int Version { get; private set; }

    public bool IsLive => RevokedAt is null;

    /// <summary>EF Core materialization only.</summary>
    private AdminPermissionGrant()
    {
    }

    private AdminPermissionGrant(Guid grantId, string principalId, PermissionCategory category, string grantedBy, DateTimeOffset now)
    {
        GrantId = grantId;
        PrincipalId = principalId;
        Category = category;
        GrantedAt = now;
        GrantedBy = grantedBy;
        Version = 1;
    }

    /// <summary>
    /// Issues (or re-issues, after a prior revoke) a live category grant to a principal.
    /// api-contract.yaml POST /admin/permission-grants. The DB's partial unique index is the
    /// actual race-safety net for "at most one live grant per (principal, category)" — the
    /// caller (repository) is expected to have already checked for a live row and this factory
    /// does not re-check, since that check is a read against other rows, not this aggregate's
    /// own invariant.
    /// </summary>
    public static Result<AdminPermissionGrant> Issue(string principalId, PermissionCategory category, string grantedBy, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(principalId))
        {
            return Result.Failure<AdminPermissionGrant>(Error.Validation("principalId is required."));
        }

        if (string.IsNullOrWhiteSpace(grantedBy))
        {
            return Result.Failure<AdminPermissionGrant>(Error.Validation("grantedBy is required."));
        }

        return Result.Success(new AdminPermissionGrant(Guid.NewGuid(), principalId.Trim(), category, grantedBy.Trim(), now));
    }

    /// <summary>
    /// Revokes this grant — an UPDATE, never a DELETE (grant/revoke history is retained for
    /// audit). Rejects if already revoked. Concurrency conflicts with another concurrent
    /// revoke/re-issue are caught by EF Core's optimistic-concurrency check on <see cref="Version"/>
    /// at SaveChanges time (design-decisions.md, "Concurrency Control for Back-Office Writes"),
    /// not here.
    /// </summary>
    public Result Revoke(string revokedBy, DateTimeOffset now)
    {
        if (!IsLive)
        {
            return Result.Failure(Error.NotFound($"Grant '{GrantId}' is not found or already revoked."));
        }

        if (string.IsNullOrWhiteSpace(revokedBy))
        {
            return Result.Failure(Error.Validation("revokedBy is required."));
        }

        RevokedAt = now;
        RevokedBy = revokedBy.Trim();
        Version++;
        return Result.Success();
    }
}
