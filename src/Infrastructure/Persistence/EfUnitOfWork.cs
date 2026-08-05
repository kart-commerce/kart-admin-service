using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KartAdminService.Infrastructure.Persistence;

/// <summary>
/// Translates a stale optimistic-concurrency write (DbUpdateConcurrencyException — the DB-level
/// safety net behind the handler's own explicit If-Match precondition check,
/// design-decisions.md "Concurrency Control for Back-Office Writes") into Error.Conflict, so
/// Application never needs to reference an EF Core exception type directly (coding-standards.md
/// DIP).
/// </summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly AdminDbContext _dbContext;

    public EfUnitOfWork(AdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(Error.Conflict("This record was modified by another request. Re-read and retry."));
        }
    }
}
