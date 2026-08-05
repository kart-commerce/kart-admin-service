using FluentAssertions;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;
using KartAdminService.Infrastructure.Persistence;
using KartAdminService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace KartAdminService.IntegrationTests;

/// <summary>
/// Proves database-design.md's Row-Level Security policy on admin_permission_grants actually
/// filters rows — not just that the SQL parses. Postgres never restricts a table's owner or any
/// superuser (regardless of `FORCE ROW LEVEL SECURITY`), and every Kart service's local-dev
/// connection is the shared `postgres` superuser (kart-devops' postgres-init.sql) - so this test
/// deliberately creates and connects as a genuine non-superuser, non-owner role, the only way to
/// actually exercise the policy rather than trivially bypass it.
/// </summary>
public sealed class RowLevelSecurityTests : IAsyncLifetime
{
    private const string AppRole = "admin_service_app_test";
    private const string AppRolePassword = "test-role-password";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("kart_admin_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private string _superuserConnectionString = null!;
    private string _appRoleConnectionString = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _superuserConnectionString = _postgres.GetConnectionString();

        await using (var migrationContext = CreateContext(_superuserConnectionString))
        {
            await migrationContext.Database.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(_superuserConnectionString);
        await connection.OpenAsync();
        await using (var createRole = connection.CreateCommand())
        {
            createRole.CommandText = $"CREATE ROLE {AppRole} LOGIN PASSWORD '{AppRolePassword}';";
            await createRole.ExecuteNonQueryAsync();
        }

        await using (var grant = connection.CreateCommand())
        {
            grant.CommandText = $"GRANT SELECT, INSERT, UPDATE ON admin_permission_grants, admin_actions TO {AppRole};";
            await grant.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlConnectionStringBuilder(_superuserConnectionString)
        {
            Username = AppRole,
            Password = AppRolePassword,
        };
        _appRoleConnectionString = builder.ConnectionString;
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private static AdminDbContext CreateContext(string connectionString, string principalId = "system:test")
    {
        var options = new DbContextOptionsBuilder<AdminDbContext>().UseNpgsql(connectionString).Options;
        return new AdminDbContext(options, new FakeCurrentPrincipal(principalId));
    }

    [Fact]
    public async Task NonSuperuserRole_CanAlwaysSeeItsOwnGrantRow()
    {
        var now = DateTimeOffset.UtcNow;
        Guid grantId;

        await using (var seedContext = CreateContext(_superuserConnectionString, "seed-script"))
        {
            var grant = AdminPermissionGrant.Issue("lone-principal", PermissionCategory.CatalogManagement, "seed-script", now).Value;
            seedContext.PermissionGrants.Add(grant);
            await seedContext.SaveChangesAsync();
            grantId = grant.GrantId;
        }

        await using var context = CreateContext(_appRoleConnectionString, "lone-principal");
        var repository = new PermissionGrantRepository(context);

        var found = await repository.GetByIdAsync(grantId, CancellationToken.None);

        found.Should().NotBeNull("a principal can always see its own grant row, regardless of any other permission");
    }

    [Fact]
    public async Task NonSuperuserRole_WithoutAPermissionManagementGrant_CannotSeeAnotherPrincipalsRow()
    {
        var now = DateTimeOffset.UtcNow;
        Guid grantId;

        await using (var seedContext = CreateContext(_superuserConnectionString, "seed-script"))
        {
            var grant = AdminPermissionGrant.Issue("target-principal", PermissionCategory.CatalogManagement, "seed-script", now).Value;
            seedContext.PermissionGrants.Add(grant);
            await seedContext.SaveChangesAsync();
            grantId = grant.GrantId;
        }

        // "unprivileged-principal" holds no grant of its own at all, let alone a
        // permission-management one - the RLS policy's own-row clause doesn't match (different
        // principal_id) and the EXISTS clause doesn't match (no permission-management row), so
        // the row must be invisible even though it genuinely exists in the table.
        await using var context = CreateContext(_appRoleConnectionString, "unprivileged-principal");
        var repository = new PermissionGrantRepository(context);

        var found = await repository.GetByIdAsync(grantId, CancellationToken.None);

        found.Should().BeNull("RLS must hide another principal's row from a caller with no permission-management grant of their own");
    }

    [Fact]
    public async Task NonSuperuserRole_HoldingALivePermissionManagementGrant_CanSeeAnotherPrincipalsRow()
    {
        var now = DateTimeOffset.UtcNow;
        Guid targetGrantId;

        await using (var seedContext = CreateContext(_superuserConnectionString, "seed-script"))
        {
            var metaGrant = AdminPermissionGrant.Issue("grant-manager", PermissionCategory.PermissionManagement, "seed-script", now).Value;
            var targetGrant = AdminPermissionGrant.Issue("target-principal", PermissionCategory.CatalogManagement, "seed-script", now).Value;
            seedContext.PermissionGrants.AddRange(metaGrant, targetGrant);
            await seedContext.SaveChangesAsync();
            targetGrantId = targetGrant.GrantId;
        }

        await using var context = CreateContext(_appRoleConnectionString, "grant-manager");
        var repository = new PermissionGrantRepository(context);

        var found = await repository.GetByIdAsync(targetGrantId, CancellationToken.None);

        found.Should().NotBeNull("a live permission-management grant must let its holder see every principal's row, per the RLS policy's EXISTS clause");
    }

    [Fact]
    public async Task NonSuperuserRole_WhoseOwnPermissionManagementGrantWasRevoked_LosesVisibilityImmediately()
    {
        var now = DateTimeOffset.UtcNow;
        Guid targetGrantId;

        await using (var seedContext = CreateContext(_superuserConnectionString, "seed-script"))
        {
            var metaGrant = AdminPermissionGrant.Issue("grant-manager", PermissionCategory.PermissionManagement, "seed-script", now).Value;
            var targetGrant = AdminPermissionGrant.Issue("target-principal", PermissionCategory.CatalogManagement, "seed-script", now).Value;
            seedContext.PermissionGrants.AddRange(metaGrant, targetGrant);
            await seedContext.SaveChangesAsync();
            targetGrantId = targetGrant.GrantId;

            // Revoke the grant-manager's own meta-grant - edge-cases.md's "Stale Admin
            // Permission Outliving an Identity-Side Revocation": the fine-grained layer is
            // looked up live, so this must take effect on the very next request, no exposure
            // window.
            metaGrant.Revoke("seed-script", now.AddSeconds(1));
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(_appRoleConnectionString, "grant-manager");
        var repository = new PermissionGrantRepository(context);

        var found = await repository.GetByIdAsync(targetGrantId, CancellationToken.None);

        found.Should().BeNull("once the caller's own permission-management grant is revoked, RLS must immediately stop showing other principals' rows");
    }
}
