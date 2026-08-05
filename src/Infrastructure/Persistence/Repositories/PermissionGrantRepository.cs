using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;
using Microsoft.EntityFrameworkCore;

namespace KartAdminService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Every read method here runs inside <see cref="AdminDbContext.BeginPrincipalScopeAsync"/> —
/// admin_permission_grants has Row-Level Security enabled (database-design.md), and its policy
/// evaluates `current_setting('app.current_principal')`, which only exists for the duration of
/// that scope. Without it, an unset setting hides every row (including the caller's own),
/// which would make every fine-grained authorization check fail closed incorrectly rather than
/// correctly evaluating "is this the caller's own row, or does the caller hold a live
/// permission-management grant."
/// </summary>
public sealed class PermissionGrantRepository : IPermissionGrantRepository
{
    private readonly AdminDbContext _dbContext;

    public PermissionGrantRepository(AdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminPermissionGrant?> GetLiveAsync(string principalId, PermissionCategory category, CancellationToken cancellationToken)
    {
        await using var scope = await _dbContext.BeginPrincipalScopeAsync(cancellationToken);
        return await _dbContext.PermissionGrants
            .Where(g => g.PrincipalId == principalId && g.Category == category && g.RevokedAt == null)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasLiveGrantAsync(string principalId, PermissionCategory category, CancellationToken cancellationToken)
    {
        await using var scope = await _dbContext.BeginPrincipalScopeAsync(cancellationToken);
        return await _dbContext.PermissionGrants
            .AnyAsync(g => g.PrincipalId == principalId && g.Category == category && g.RevokedAt == null, cancellationToken);
    }

    public async Task<AdminPermissionGrant?> GetByIdAsync(Guid grantId, CancellationToken cancellationToken)
    {
        await using var scope = await _dbContext.BeginPrincipalScopeAsync(cancellationToken);
        return await _dbContext.PermissionGrants.SingleOrDefaultAsync(g => g.GrantId == grantId, cancellationToken);
    }

    public async Task AddAsync(AdminPermissionGrant grant, CancellationToken cancellationToken) =>
        await _dbContext.PermissionGrants.AddAsync(grant, cancellationToken);

    public async Task<(IReadOnlyList<AdminPermissionGrant> Items, int Total)> ListAsync(
        string? principalId,
        PermissionCategory? category,
        bool includeRevoked,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var scope = await _dbContext.BeginPrincipalScopeAsync(cancellationToken);

        var query = _dbContext.PermissionGrants.AsQueryable();

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
            query = query.Where(g => g.RevokedAt == null);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(g => g.GrantedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
