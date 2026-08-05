using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.ListAdminActions;

/// <summary>
/// api-contract.yaml GET /admin/actions (ADM-16). Deliberately coarser than the write path —
/// requirement-spec.md §4: "no fine-grained category check applies to this read-only audit
/// view" — any caller holding the coarse Admin claim may read the full trail. The coarse-claim
/// check itself happens at the API/auth layer (policy), not here.
/// </summary>
public sealed record ListAdminActionsQuery(
    string? AdminId,
    PermissionCategory? Category,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<AdminActionResultDto>>>;
