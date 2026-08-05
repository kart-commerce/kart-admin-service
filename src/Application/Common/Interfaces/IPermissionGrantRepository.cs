using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;

namespace KartAdminService.Application.Common.Interfaces;

/// <summary>
/// Persistence abstraction for the AdminPermissionGrant aggregate (coding-standards.md DIP).
/// One repository per aggregate root — never a generic IRepository&lt;T&gt;.
/// </summary>
public interface IPermissionGrantRepository
{
    /// <summary>
    /// The uncached, per-request live-grant lookup every /admin/* handler runs
    /// (design-decisions.md, "Caching Strategy for Fine-Grained Permission Grants" — no
    /// caching, ever). Null if the principal holds no live grant for this category.
    /// </summary>
    Task<AdminPermissionGrant?> GetLiveAsync(string principalId, PermissionCategory category, CancellationToken cancellationToken);

    Task<bool> HasLiveGrantAsync(string principalId, PermissionCategory category, CancellationToken cancellationToken);

    Task<AdminPermissionGrant?> GetByIdAsync(Guid grantId, CancellationToken cancellationToken);

    Task AddAsync(AdminPermissionGrant grant, CancellationToken cancellationToken);

    /// <summary>Paginated, optionally filtered — api-contract.yaml GET /admin/permission-grants.</summary>
    Task<(IReadOnlyList<AdminPermissionGrant> Items, int Total)> ListAsync(
        string? principalId,
        PermissionCategory? category,
        bool includeRevoked,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
