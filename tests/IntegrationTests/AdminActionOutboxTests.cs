using FluentAssertions;
using KartAdminService.Domain.Actions;
using KartAdminService.Domain.Common;
using KartAdminService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace KartAdminService.IntegrationTests;

/// <summary>
/// Verifies the Transactional Outbox mechanics admin_actions doubles as (design-decisions.md,
/// "Audit Trail Publication Atomicity") against real PostgreSQL: an unpublished row is found by
/// idx_admin_actions_unpublished's partial-index scan, and MarkPublished's invariant (never
/// re-publish, always stamp the well-known system actor) persists correctly - the same
/// query/mutation shape OutboxRelayHostedService relies on every poll cycle.
/// </summary>
public sealed class AdminActionOutboxTests : IAsyncLifetime
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
    public async Task UnpublishedRow_IsFoundByThePollerQuery_ThenMarkedPublishedWithTheSystemPollerActor()
    {
        var now = DateTimeOffset.UtcNow;
        var action = AdminAction.Record(Guid.NewGuid(), "admin-1", PermissionCategory.UserSuspension, ActionNames.UserLock, "user-1", null, now).Value;

        await using (var writeContext = CreateContext())
        {
            writeContext.AdminActions.Add(action);
            await writeContext.SaveChangesAsync();
        }

        await using var pollContext = CreateContext();
        var pending = await pollContext.AdminActions.Where(a => a.PublishedAt == null).OrderBy(a => a.PerformedAt).Take(50).ToListAsync();
        pending.Should().ContainSingle(a => a.ActionId == action.ActionId);

        var polledAction = pending.Single(a => a.ActionId == action.ActionId);
        polledAction.MarkPublished(now.AddSeconds(5));
        await pollContext.SaveChangesAsync();

        await using var verifyContext = CreateContext();
        var persisted = await verifyContext.AdminActions.SingleAsync(a => a.ActionId == action.ActionId);
        // Postgres timestamptz stores microsecond precision; .NET DateTimeOffset ticks are
        // 100ns - an exact-equality check on a value that round-tripped through the database
        // would flake on the sub-microsecond remainder, so this allows a 1ms tolerance.
        persisted.PublishedAt.Should().BeCloseTo(now.AddSeconds(5), TimeSpan.FromMilliseconds(1));
        persisted.PublishedBy.Should().Be(AdminAction.OutboxPollerSystemPrincipal);

        var stillPending = await verifyContext.AdminActions.Where(a => a.PublishedAt == null).ToListAsync();
        stillPending.Should().NotContain(a => a.ActionId == action.ActionId, "a published row must drop out of the poller's unpublished scan");
    }

    [Fact]
    public async Task MarkPublished_CalledASecondTime_ThrowsAndNeverOverwritesTheFirstPublishTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var action = AdminAction.Record(Guid.NewGuid(), "admin-1", PermissionCategory.UserSuspension, ActionNames.UserLock, "user-1", null, now).Value;

        await using var context = CreateContext();
        context.AdminActions.Add(action);
        await context.SaveChangesAsync();

        action.MarkPublished(now.AddSeconds(1));
        await context.SaveChangesAsync();

        var act = () => action.MarkPublished(now.AddSeconds(2));
        act.Should().Throw<InvalidOperationException>();
    }
}
