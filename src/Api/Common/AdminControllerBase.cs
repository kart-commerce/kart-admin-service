using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

namespace KartAdminService.Api.Common;

/// <summary>Shared "resolve the acting principal from the validated JWT" helper — every one of the seven /admin/* controllers needs it, so it lives here once rather than duplicated per controller.</summary>
public abstract class AdminControllerBase : ControllerBase
{
    protected string ActingPrincipalId => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "system:unknown";
}
