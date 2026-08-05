using KartAdminService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KartAdminService.Api.HealthChecks;

/// <summary>
/// Readiness signal for the k8s Helm chart's /health/ready probe — a database that is reachable
/// but behind on migrations (e.g. admin_actions never created) must fail readiness too, not
/// just an unreachable one, so a pod never accepts traffic while OutboxRelayHostedService is
/// looping on errors (kart-conventions.md, "Database Migrations & Startup Readiness").
/// </summary>
public sealed class AdminDbHealthCheck(AdminDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();

            return pending.Length == 0
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"{pending.Length} pending migration(s): {string.Join(", ", pending)}");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Admin database is unreachable", exception);
        }
    }
}
