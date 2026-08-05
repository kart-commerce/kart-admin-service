using FluentAssertions;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;
using KartAdminService.Infrastructure.Persistence;
using KartAdminService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace KartAdminService.IntegrationTests;

/// <summary>
/// Exercises optimistic-concurrency conflict resolution (design-decisions.md, "Concurrency
/// Control for Back-Office Writes") against real PostgreSQL: two concurrent revokes of the same
/// grant must not both succeed — exactly one wins, the other observes a concurrency conflict
/// (never a silent overwrite of the first admin's change). Runs as the Postgres superuser (the
/// same role every Kart service's local-dev connection uses), so this does NOT prove Row-Level
/// Security enforcement itself — RLS.FORCE never restricts a superuser, by Postgres design.
/// RowLevelSecurityTests.cs proves the policy expression itself, under a genuine non-superuser
/// role. This file proves the concurrency-token mechanics only.
/// </summary>
public sealed class PermissionGrantConcurrencyTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("kart_admin_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();

        await using var migrationContext = CreateContext();
        await migrationContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private AdminDbContext CreateContext(string principalId = "system:test")
    {
        var options = new DbContextOptionsBuilder<AdminDbContext>().UseNpgsql(_connectionString).Options;
        return new AdminDbContext(options, new FakeCurrentPrincipal(principalId));
    }

    [Fact]
    public async Task ConcurrentRevokes_OfTheSameGrant_ExactlyOneSucceeds()
    {
        var now = DateTimeOffset.UtcNow;
        Guid targetGrantId;

        await using (var seedContext = CreateContext("seed-script"))
        {
            // Both acting admins need their own live permission-management grant for the RLS
            // policy's "or holds a live permission-management grant" clause to let them see
            // (and revoke) a *different* principal's row - the same rule the application layer
            // itself checks (Domain Invariant #1).
            var adminAMeta = AdminPermissionGrant.Issue("admin-a", PermissionCategory.PermissionManagement, "seed-script", now).Value;
            var adminBMeta = AdminPermissionGrant.Issue("admin-b", PermissionCategory.PermissionManagement, "seed-script", now).Value;
            var targetGrant = AdminPermissionGrant.Issue("target-principal", PermissionCategory.CatalogManagement, "seed-script", now).Value;
            seedContext.PermissionGrants.AddRange(adminAMeta, adminBMeta, targetGrant);
            await seedContext.SaveChangesAsync();
            targetGrantId = targetGrant.GrantId;
        }

        // Two independent contexts/repositories each load the same live grant (version 1) -
        // mirroring two concurrent admins, each having read the grant before either one writes.
        await using var contextA = CreateContext("admin-a");
        await using var contextB = CreateContext("admin-b");
        var repositoryA = new PermissionGrantRepository(contextA);
        var repositoryB = new PermissionGrantRepository(contextB);

        var grantA = await repositoryA.GetByIdAsync(targetGrantId, CancellationToken.None);
        var grantB = await repositoryB.GetByIdAsync(targetGrantId, CancellationToken.None);
        grantA.Should().NotBeNull("admin-a holds a live permission-management grant, so RLS must let it see target-principal's row");
        grantB.Should().NotBeNull("admin-b holds a live permission-management grant, so RLS must let it see target-principal's row");

        grantA!.Revoke("admin-a", now.AddMinutes(1));
        grantB!.Revoke("admin-b", now.AddMinutes(2));

        var unitOfWorkA = new EfUnitOfWork(contextA);
        var unitOfWorkB = new EfUnitOfWork(contextB);

        var resultA = await unitOfWorkA.SaveChangesAsync(CancellationToken.None);
        var resultB = await unitOfWorkB.SaveChangesAsync(CancellationToken.None);

        var results = new[] { resultA, resultB };
        results.Count(r => r.IsSuccess).Should().Be(1, "exactly one concurrent revoke must win");
        results.Count(r => r.IsFailure && r.Error.Code == "conflict").Should().Be(1, "the losing writer must observe a conflict, never a silent overwrite");

        await using var verifyContext = CreateContext("seed-script");
        var verifyRepository = new PermissionGrantRepository(verifyContext);
        var finalGrant = await verifyRepository.GetByIdAsync(targetGrantId, CancellationToken.None);
        finalGrant!.Version.Should().Be(2, "only one revoke actually committed, so the version advanced exactly once");
    }
}
