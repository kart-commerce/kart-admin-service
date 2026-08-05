namespace KartAdminService.Application.Common.Interfaces;

/// <summary>
/// Resolves the acting principal id from the caller's Identity-issued access token `sub` claim
/// (kart-identity-service's JwtAccessTokenGenerator). Implemented in Infrastructure
/// (HttpCurrentPrincipal, same pattern as kart-category-service) so Application never
/// references HttpContext directly.
/// </summary>
public interface ICurrentPrincipal
{
    string PrincipalId { get; }
}
