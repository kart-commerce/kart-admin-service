using Kart.Shared.Configuration;
using Kart.Shared.ErrorHandling;
using Kart.Shared.Observability;
using KartAdminService.Api;
using KartAdminService.Api.HealthChecks;
using KartAdminService.Api.Middleware;
using KartAdminService.Api.Security;
using KartAdminService.Application;
using KartAdminService.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// kart-conventions.md Configuration Management: GlobalConfig external-secrets-file bootstrap,
// shared across every service - never reimplemented per service. See appsettings.Local.json.example.
builder.AddKartGlobalConfig();

// kart-conventions.md Observability section: Serilog + OpenTelemetry SDK behind one DI call.
builder.AddKartObservability("kart-admin-service");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// /health/live: process is up, no dependency check. /health/ready: this service's job depends
// on Postgres being reachable AND migrated - matching kart-infra's service-chart probe
// convention. No Redis/Mongo checks - this service's approved design has neither.
builder.Services.AddHealthChecks()
    .AddCheck<AdminDbHealthCheck>("admin-db", tags: ["ready"]);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAdminAuthentication();

// design-decisions.md, "Global Exception Handling & Consistent Response Model": Admin Service
// is the first adopter of Kart.Shared.ErrorHandling (unlike kart-identity-service/
// kart-category-service, which predate this package and still hand-roll their own
// GlobalExceptionHandler). Domain/business errors keep using the Result/Either pattern
// (ResultExtensions.MapFailure) - no exception mappings are registered here because every
// expected business outcome (permission_denied, not_found, conflict, downstream_unavailable,
// validation_error) already flows through Result, never an exception. This handler exists
// purely for the genuinely exceptional case: an unhandled infrastructure fault.
builder.Services.AddKartErrorHandling();

var app = builder.Build();

await StartupConnectivityChecks.RunAsync(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Per-HTTP-request Information log (method/path/status/elapsed) - the RED-style access log
// observability-standards.md expects on every endpoint, for free. Registered outermost,
// wrapping UseKartErrorHandling below, so this always logs the *final* status code a client
// actually received.
app.UseSerilogRequestLogging();

// The single global error handler (Kart.Shared.ErrorHandling) - every unhandled exception is
// translated to the platform's ProblemDetails envelope and logged here, so no controller/
// handler needs its own try/catch.
app.UseKartErrorHandling();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<AdminContextEnrichmentMiddleware>();
app.UseAuthorization();

// Prometheus scrape target (observability-standards.md's mandatory `/metrics`).
app.MapPrometheusScrapingEndpoint();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory<Program> in IntegrationTests/ContractTests.
public partial class Program
{
}
