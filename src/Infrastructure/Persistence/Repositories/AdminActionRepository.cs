using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Domain.Actions;
using KartAdminService.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace KartAdminService.Infrastructure.Persistence.Repositories;

public sealed class AdminActionRepository : IAdminActionRepository
{
    private const string UniqueViolationSqlState = "23505";

    private readonly AdminDbContext _dbContext;
    private readonly ILogger<AdminActionRepository> _logger;

    public AdminActionRepository(AdminDbContext dbContext, ILogger<AdminActionRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<AdminAction?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken) =>
        _dbContext.AdminActions.SingleOrDefaultAsync(a => a.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<AdminAction> AddAndCommitOrGetExistingAsync(AdminAction action, CancellationToken cancellationToken)
    {
        await _dbContext.AdminActions.AddAsync(action, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return action;
        }
        catch (DbUpdateException ex) when (IsUniqueIdempotencyKeyViolation(ex))
        {
            // A concurrent request with the same Idempotency-Key committed first between the
            // caller's own replay check and this insert — uq_admin_actions_idempotency_key is
            // the true race-safety net, not the read-then-write check alone. Detach the losing
            // in-memory entity (it was never actually persisted) and hand back the row the
            // other, faster request just committed.
            _dbContext.Entry(action).State = EntityState.Detached;

            _logger.LogInformation(
                "Concurrent duplicate insert for Idempotency-Key {IdempotencyKey}; re-fetching the winning row.",
                action.IdempotencyKey);

            var winner = await GetByIdempotencyKeyAsync(action.IdempotencyKey, cancellationToken);
            return winner ?? throw new InvalidOperationException(
                $"Unique-violation on Idempotency-Key {action.IdempotencyKey} but no row could be found afterward — this should be unreachable.");
        }
    }

    public async Task<(IReadOnlyList<AdminAction> Items, int Total)> ListAsync(
        string? adminId,
        PermissionCategory? category,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.AdminActions.AsQueryable();

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

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    private static bool IsUniqueIdempotencyKeyViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: UniqueViolationSqlState } postgresException &&
        postgresException.ConstraintName == "uq_admin_actions_idempotency_key";
}
