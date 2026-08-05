using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.ListPermissionGrants;

/// <summary>
/// api-contract.yaml GET /admin/permission-grants (ADM-3). CanRead here is scoped by the RLS
/// policy (database-design.md): a principal always sees its own rows, plus every row if it holds
/// a live permission-management grant — enforced at the database layer (RLS), not re-checked here
/// (Handle trusts whatever rows the connection's RLS session returns).
/// </summary>
public sealed record ListPermissionGrantsQuery(
    string? PrincipalId,
    PermissionCategory? Category,
    bool IncludeRevoked,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<PermissionGrantDto>>>;
