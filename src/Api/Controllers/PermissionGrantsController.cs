using KartAdminService.Api.Common;
using KartAdminService.Api.Security;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.IssuePermissionGrant;
using KartAdminService.Application.Features.ListPermissionGrants;
using KartAdminService.Application.Features.RevokePermissionGrant;
using KartAdminService.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KartAdminService.Api.Controllers;

/// <summary>api-contract.yaml /admin/permission-grants* (ADM-1, ADM-2, ADM-3) — the permission-management meta-category.</summary>
[ApiController]
[Route("v1/admin/permission-grants")]
[Authorize(Policy = AuthenticationExtensions.AdminPolicy)]
public sealed class PermissionGrantsController : AdminControllerBase
{
    private readonly ISender _sender;

    public PermissionGrantsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PermissionGrantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PermissionGrantDto>>> ListPermissionGrants(
        [FromQuery] string? principalId,
        [FromQuery] string? category,
        [FromQuery] bool includeRevoked = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var parsedCategory = category is null ? (PermissionCategory?)null : PermissionCategoryExtensions.ParseWireValue(category);
        var result = await _sender.Send(new ListPermissionGrantsQuery(principalId, parsedCategory, includeRevoked, page, pageSize), cancellationToken);
        return this.ToActionResult<PagedResult<PermissionGrantDto>, PagedResult<PermissionGrantDto>>(result, r => Ok(r));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PermissionGrantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PermissionGrantDto>> IssuePermissionGrant(
        [FromBody] IssuePermissionGrantRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new IssuePermissionGrantCommand(
            ActingPrincipalId,
            request.PrincipalId,
            PermissionCategoryExtensions.ParseWireValue(request.Category),
            idempotencyKey);

        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<PermissionGrantDto, PermissionGrantDto>(
            result,
            grant => CreatedAtAction(nameof(ListPermissionGrants), new { principalId = grant.PrincipalId }, grant));
    }

    [HttpPost("{grantId:guid}/revoke")]
    [ProducesResponseType(typeof(PermissionGrantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PermissionGrantDto>> RevokePermissionGrant(
        [FromRoute] Guid grantId,
        [FromHeader(Name = "If-Match")] int ifMatch,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new RevokePermissionGrantCommand(ActingPrincipalId, grantId, ifMatch, idempotencyKey);
        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<PermissionGrantDto, PermissionGrantDto>(result, r => Ok(r));
    }
}

/// <summary>api-contract.yaml issuePermissionGrant requestBody shape.</summary>
public sealed record IssuePermissionGrantRequest(string PrincipalId, string Category);
