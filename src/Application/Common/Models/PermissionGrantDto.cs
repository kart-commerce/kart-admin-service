using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;

namespace KartAdminService.Application.Common.Models;

/// <summary>api-contract.yaml PermissionGrant schema.</summary>
public sealed record PermissionGrantDto(
    Guid GrantId,
    string PrincipalId,
    string Category,
    DateTimeOffset GrantedAt,
    string GrantedBy,
    DateTimeOffset? RevokedAt,
    string? RevokedBy,
    int Version)
{
    public static PermissionGrantDto FromDomain(AdminPermissionGrant grant) => new(
        grant.GrantId,
        grant.PrincipalId,
        grant.Category.ToWireValue(),
        grant.GrantedAt,
        grant.GrantedBy,
        grant.RevokedAt,
        grant.RevokedBy,
        grant.Version);
}
