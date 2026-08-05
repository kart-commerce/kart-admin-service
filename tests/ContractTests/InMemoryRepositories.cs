using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Domain.Actions;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;

namespace KartAdminService.ContractTests;

/// <summary>
/// In-memory stand-ins for the real EF-backed repositories - these contract tests assert the
/// HTTP wire contract (status codes, JSON field names, RBAC/Idempotency-Key header gating)
/// against api-contract.yaml, not persistence/RLS mechanics (already covered by IntegrationTests).
/// </summary>
public sealed class InMemoryPermissionGrantRepository : IPermissionGrantRepository
{
    public List<AdminPermissionGrant> Grants { get; } = new();

    public Task<AdminPermissionGrant?> GetLiveAsync(string principalId, PermissionCategory category, CancellationToken cancellationToken) =>
        Task.FromResult(Grants.SingleOrDefault(g => g.PrincipalId == principalId && g.Category == category && g.IsLive));

    public Task<bool> HasLiveGrantAsync(string principalId, PermissionCategory category, CancellationToken cancellationToken) =>
        Task.FromResult(Grants.Any(g => g.PrincipalId == principalId && g.Category == category && g.IsLive));

    public Task<AdminPermissionGrant?> GetByIdAsync(Guid grantId, CancellationToken cancellationToken) =>
        Task.FromResult(Grants.SingleOrDefault(g => g.GrantId == grantId));

    public Task AddAsync(AdminPermissionGrant grant, CancellationToken cancellationToken)
    {
        Grants.Add(grant);
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<AdminPermissionGrant> Items, int Total)> ListAsync(
        string? principalId, PermissionCategory? category, bool includeRevoked, int page, int pageSize, CancellationToken cancellationToken)
    {
        IEnumerable<AdminPermissionGrant> query = Grants;
        if (!string.IsNullOrWhiteSpace(principalId))
        {
            query = query.Where(g => g.PrincipalId == principalId);
        }

        if (category is { } cat)
        {
            query = query.Where(g => g.Category == cat);
        }

        if (!includeRevoked)
        {
            query = query.Where(g => g.IsLive);
        }

        var all = query.OrderByDescending(g => g.GrantedAt).ToList();
        var page1 = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult<(IReadOnlyList<AdminPermissionGrant>, int)>((page1, all.Count));
    }
}

public sealed class InMemoryAdminActionRepository : IAdminActionRepository
{
    public List<AdminAction> Actions { get; } = new();

    public Task<AdminAction?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken) =>
        Task.FromResult(Actions.SingleOrDefault(a => a.IdempotencyKey == idempotencyKey));

    public Task<AdminAction> AddAndCommitOrGetExistingAsync(AdminAction action, CancellationToken cancellationToken)
    {
        var existing = Actions.SingleOrDefault(a => a.IdempotencyKey == action.IdempotencyKey);
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        Actions.Add(action);
        return Task.FromResult(action);
    }

    public Task<(IReadOnlyList<AdminAction> Items, int Total)> ListAsync(
        string? adminId, PermissionCategory? category, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken)
    {
        IEnumerable<AdminAction> query = Actions;
        if (!string.IsNullOrWhiteSpace(adminId))
        {
            query = query.Where(a => a.AdminId == adminId);
        }

        if (category is { } cat)
        {
            query = query.Where(a => a.Category == cat);
        }

        if (from is { } fromValue)
        {
            query = query.Where(a => a.PerformedAt >= fromValue);
        }

        if (to is { } toValue)
        {
            query = query.Where(a => a.PerformedAt <= toValue);
        }

        var all = query.OrderByDescending(a => a.PerformedAt).ToList();
        var page1 = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult<(IReadOnlyList<AdminAction>, int)>((page1, all.Count));
    }
}

/// <summary>Grants/revokes committed in-process by InMemoryPermissionGrantRepository already - no real transaction to commit.</summary>
public sealed class NoOpUnitOfWork : IUnitOfWork
{
    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}
