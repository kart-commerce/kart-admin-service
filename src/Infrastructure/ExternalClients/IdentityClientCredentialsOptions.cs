namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>
/// Binds the "IdentityClientCredentials" configuration section — this service's own
/// service-principal credentials for kart-identity-service's OAuth2 Client Credentials grant
/// (POST /v1/auth/token), used to call the real, confirmed
/// POST /v1/internal/users/{userId}/lock|unlock routes (ADR-0010; scoped "admin").
/// </summary>
public sealed class IdentityClientCredentialsOptions
{
    public const string SectionName = "IdentityClientCredentials";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = "admin";
}
