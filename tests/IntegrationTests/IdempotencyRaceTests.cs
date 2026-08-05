using FluentAssertions;
using KartAdminService.Domain.Actions;
using KartAdminService.Domain.Common;
using KartAdminService.Infrastructure.Persistence;
using KartAdminService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace KartAdminService.IntegrationTests;

/// <summary>
/// Exercises the concrete "no double execution" guarantee against a real PostgreSQL engine
/// (design-decisions.md, "Idempotency Mechanism for Outbound Write Calls"): two concurrent
/// attempts carrying the *same* Idempotency-Key must never both succeed in writing an
/// admin_actions row — uq_admin_actions_idempotency_key is the true race-safety net, and
/// AdminActionRepository.AddAndCommitOrGetExistingAsync must resolve the loser to the winner's
/// row rather than erroring or producing a duplicate. This is exactly the "no double payment /
/// no double coupon issuance" guarantee the user asked for, proven under a genuine concurrent
/// race, not just a sequential replay (already covered by AdminActionExecutorTests).
/// </summary>
public sealed class IdempotencyRaceTests : IAsyncLifetime
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
    public async Task AddAndCommitOrGetExistingAsync_UnderConcurrentDuplicateIdempotencyKey_ProducesExactlyOneRow()
    {
        var idempotencyKey = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Two independent DbContext instances (as two concurrent HTTP requests would each get
        // their own scoped instance), both racing to record the *same* logical admin action.
        await using var contextA = CreateContext("admin-a");
        await using var contextB = CreateContext("admin-b");
        var repositoryA = new AdminActionRepository(contextA, NullLoggerFor<AdminActionRepository>());
        var repositoryB = new AdminActionRepository(contextB, NullLoggerFor<AdminActionRepository>());

        var actionA = AdminAction.Record(idempotencyKey, "admin-a", PermissionCategory.UserSuspension, ActionNames.UserLock, "user-1", null, now).Value;
        var actionB = AdminAction.Record(idempotencyKey, "admin-b", PermissionCategory.UserSuspension, ActionNames.UserLock, "user-1", null, now).Value;

        var results = await Task.WhenAll(
            repositoryA.AddAndCommitOrGetExistingAsync(actionA, CancellationToken.None),
            repositoryB.AddAndCommitOrGetExistingAsync(actionB, CancellationToken.None));

        // Both callers must observe the SAME winning row - whichever request actually committed
        // first - never two different ActionIds.
        results[0].ActionId.Should().Be(results[1].ActionId);

        await using var verifyContext = CreateContext();
        var rowCount = await verifyContext.AdminActions.CountAsync(a => a.IdempotencyKey == idempotencyKey);
        rowCount.Should().Be(1, "the unique index on idempotency_key must make a concurrent duplicate attempt collapse to exactly one row");
    }

    private static Microsoft.Extensions.Logging.ILogger<T> NullLoggerFor<T>() => Microsoft.Extensions.Logging.Abstractions.NullLogger<T>.Instance;
}
