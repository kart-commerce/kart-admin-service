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
            .AddJwtBearer();

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
}
