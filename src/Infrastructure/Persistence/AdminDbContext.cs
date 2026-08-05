using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Domain.Actions;
using KartAdminService.Domain.PermissionGrants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KartAdminService.Infrastructure.Persistence;

/// <summary>
/// Owns exactly the two tables Admin's own approved design docs allow (Domain Invariant #3 —
/// never a second owner of another service's domain data): admin_permission_grants, admin_actions.
/// </summary>
public sealed class AdminDbContext : DbContext
{
    private readonly ICurrentPrincipal? _currentPrincipal;

    public DbSet<AdminPermissionGrant> PermissionGrants => Set<AdminPermissionGrant>();
    public DbSet<AdminAction> AdminActions => Set<AdminAction>();

    /// <summary>
    /// ICurrentPrincipal is optional (null in design-time/migration contexts —
    /// AdminDbContextFactory constructs this DbContext directly, with no HTTP request, no DI
    /// container providing HttpCurrentPrincipal). At runtime the Api host always registers a
    /// real ICurrentPrincipal, so both <see cref="SaveChangesAsync(CancellationToken)"/> and
    /// <see cref="BeginPrincipalScopeAsync"/> always have one there.
    /// </summary>
    public AdminDbContext(DbContextOptions<AdminDbContext> options, ICurrentPrincipal? currentPrincipal = null)
        : base(options)
    {
        _currentPrincipal = currentPrincipal;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);
    }

    /// <summary>
    /// database-design.md's Row-Level Security Policy requires the session-scoped
    /// `app.current_principal` setting to be live for the RLS predicate to evaluate against the
    /// calling principal (same ambient mechanism kart-identity-service's database-design.md
    /// establishes). `SET LOCAL` only lasts the current transaction, so every write goes through
    /// this override, which issues it inside the same explicit transaction as the actual write —
    /// never as a separate round trip that would have already committed (and lost the setting)
    /// before SaveChanges' own work runs.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction is not null)
        {
            // Already inside a caller-managed transaction (e.g. PermissionGrantRepository's own
            // read scope, below) — SET LOCAL already applies to that outer transaction, so
            // re-entering here would be redundant.
            return await base.SaveChangesAsync(cancellationToken);
        }

        await using var transaction = await BeginPrincipalScopeAsync(cancellationToken);
        var affected = await base.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return affected;
    }

    /// <summary>
    /// Opens an explicit transaction with `app.current_principal` set for its duration — the
    /// same mechanism <see cref="SaveChangesAsync(CancellationToken)"/> uses for writes, exposed
    /// here so read paths that never call SaveChanges (PermissionGrantRepository's list/get
    /// queries) still run under a session where the RLS policy on admin_permission_grants can
    /// evaluate `current_setting('app.current_principal')` correctly. Callers dispose the
    /// returned transaction without committing (a plain read has nothing to persist) — disposing
    /// an uncommitted transaction rolls it back, which is a no-op for a read-only scope.
    /// </summary>
    public async Task<IDbContextTransaction> BeginPrincipalScopeAsync(CancellationToken cancellationToken)
    {
        var principalId = _currentPrincipal?.PrincipalId ?? "system:unknown";
        var transaction = await Database.BeginTransactionAsync(cancellationToken);

        // Postgres's `SET LOCAL x = $1` is not valid SQL — SET only accepts a literal, never a
        // bind parameter, so a naive ExecuteSqlInterpolatedAsync("SET LOCAL ... = {value}")
        // fails with a syntax error. set_config(name, value, is_local) is the parameterized
        // equivalent — a plain function call, so it takes a bound parameter normally, and
        // is_local=true gives the identical "lasts only this transaction" behavior as SET LOCAL.
        await Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('app.current_principal', {principalId}, true)", cancellationToken);
        return transaction;
    }
}
