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
        

        // Optional: Add events for debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // <--For debugging what is getting inside the claims in Authorize attribute 
                Console.WriteLine("Token validated successfully");
                var claims = context?.Principal?.Claims.ToList();
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"OnChallenge: {context.Error}, {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    }
}