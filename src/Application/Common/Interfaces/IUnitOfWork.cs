using Kart.Shared.Domain;

namespace KartAdminService.Application.Common.Interfaces;

/// <summary>
/// Commits the PostgreSQL transaction for the current call. ddd-model.md's Cross-Aggregate
/// Interaction section requires grant-issue/revoke's own paired AdminAction row to be a
/// *separate* local commit from the AdminPermissionGrant write, never one wrapping transaction —
/// so handlers call SaveChangesAsync twice rather than opening an explicit outer transaction.
///
/// Returns a <see cref="Result"/> rather than throwing so Application stays persistence-provider
/// agnostic (coding-standards.md DIP): a stale optimistic-concurrency write
/// (design-decisions.md, "Concurrency Control for Back-Office Writes") is translated to
/// <c>Error.Conflict</c> by Infrastructure's EfUnitOfWork, which alone knows about
/// DbUpdateConcurrencyException — Application never catches an EF Core exception type directly.
/// </summary>
public interface IUnitOfWork
{
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
