using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KartAdminService.Infrastructure.Persistence;

/// <summary>
/// Design-time-only factory `dotnet ef migrations add`/`database update` use to build
/// <see cref="AdminDbContext"/> without spinning up the full Api host (and its own required
/// configuration, e.g. GlobalConfig/JWT). Never used at runtime — the app's own DI registration
/// (Infrastructure/DependencyInjection.cs) takes over there. Mirrors
/// kart-category-service's CategoryDbContextFactory.cs exactly (kart-conventions.md's Database
/// Migrations & Startup Readiness section).
/// </summary>
public sealed class AdminDbContextFactory : IDesignTimeDbContextFactory<AdminDbContext>
{
    public AdminDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ADMIN_DB_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=kart_admin;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AdminDbContext(optionsBuilder.Options);
    }
}
