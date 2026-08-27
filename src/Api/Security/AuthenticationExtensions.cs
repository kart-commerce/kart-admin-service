using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace KartAdminService.Api.Security;

/// <summary>
/// api-contract.yaml's bearerAuth security scheme: an Identity-issued RS256 JWT carrying the
/// `Admin` role claim, coarse-validated at the API Gateway before the request reaches this
/// service (BRD §24.1's two-tier enforcement) and re-checked here structurally (signature +
/// expiry) — Admin Service never re-derives role grants locally, and never re-implements
/// Identity's own coarse revocation-list check (edge-cases.md, "Stale Admin Permission
/// Outliving an Identity-Side Revocation"). "AdminOnly" gates every /admin/* endpoint on
/// kart-identity-service's actual claim shape: `new Claim("roles", role)`, value "admin".
/// </summary>
public static class AuthenticationExtensions
{
    public const string AdminPolicy = "AdminOnly";
    private const string RolesClaimType = "roles";
    private const string AdminRoleValue = "admin";

    public static IServiceCollection AddAdminAuthentication(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient<JwksSigningKeyResolver>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(SetJwtBearerOptions);

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwksSigningKeyResolver>((options, resolver) =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Identity's JwtAccessTokenGenerator sets neither `iss` nor `aud` on the
                    // tokens it mints - validating either here would reject every real token.
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeyResolver = resolver.ResolveSigningKeys,
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AdminPolicy, policy => policy.RequireClaim(RolesClaimType, AdminRoleValue));

        return services;
    }

    private static void SetJwtBearerOptions(JwtBearerOptions options)
    {
        // IMPORTANT: Disable claim type mapping on the handler itself
        // This helps to keep JWT claim names (like "sub") unchanged instead of converting to long XML URIs
        // Like "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" instead of "sub"
        options.MapInboundClaims = false;


        // Structured, through the same Serilog/OTel pipeline every other log line in this
        // service goes through (kart-conventions.md: "never string-concatenated messages") -
        // these three events previously bypassed it entirely via Console.WriteLine, which meant
        // an auth failure/challenge on the highest-privilege /admin/* surface never reached Loki
        // or carried a TraceId at all.
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("KartAdminService.Api.Security.Authentication");
                logger.LogWarning(context.Exception, "Stage {Stage}: JWT authentication failed on {Path}", "AuthenticationFailed", context.Request.Path);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("KartAdminService.Api.Security.Authentication");
                logger.LogDebug("Stage {Stage}: JWT validated for subject {Subject} on {Path}", "AuthenticationSucceeded", context.Principal?.FindFirst("sub")?.Value, context.Request.Path);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("KartAdminService.Api.Security.Authentication");
                logger.LogWarning("Stage {Stage}: JWT challenge on {Path} - {Error} {ErrorDescription}", "AuthenticationChallenged", context.Request.Path, context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    }
}