using KartAdminService.Api.Common;
using KartAdminService.Api.Security;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.ListAdminActions;
using KartAdminService.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KartAdminService.Api.Controllers;

/// <summary>
/// api-contract.yaml GET /admin/actions (ADM-16) — Admin's own append-only audit trail.
/// Deliberately coarser than the write path: any caller holding the coarse Admin claim may
/// read it, with no fine-grained category check (requirement-spec.md §4) — the [Authorize]
/// policy below is the only gate, matching that rule exactly.
/// </summary>
[ApiController]
[Route("v1/admin/actions")]
[Authorize(Policy = AuthenticationExtensions.AdminPolicy)]
public sealed class ActionsController : AdminControllerBase
{
    private readonly ISender _sender;

    public ActionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminActionResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminActionResultDto>>> ListAdminActions(
        [FromQuery] string? adminId,
        [FromQuery] string? category,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var parsedCategory = category is null ? (PermissionCategory?)null : PermissionCategoryExtensions.ParseWireValue(category);
        var result = await _sender.Send(new ListAdminActionsQuery(adminId, parsedCategory, from, to, page, pageSize), cancellationToken);
        return this.ToActionResult<PagedResult<AdminActionResultDto>, PagedResult<AdminActionResultDto>>(result, r => Ok(r));
    }
}
