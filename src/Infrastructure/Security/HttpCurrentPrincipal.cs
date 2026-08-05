using System.IdentityModel.Tokens.Jwt;
using KartAdminService.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace KartAdminService.Infrastructure.Security;

/// <summary>
/// Resolves the acting principal from the caller's Identity-issued access token `sub` claim
/// (kart-identity-service's JwtAccessTokenGenerator), same pattern as kart-category-service's
/// HttpCurrentPrincipal. Falls back to a well-known system id outside an HTTP request context
/// (there is none for this service — every /admin/* action is API-triggered — but this keeps
/// the contract total for e.g. a future CLI/background caller).
/// </summary>
public sealed class HttpCurrentPrincipal : ICurrentPrincipal
{
    private const string UnknownPrincipal = "system:unknown";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentPrincipal(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string PrincipalId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? UnknownPrincipal;
}
