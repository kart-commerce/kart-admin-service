using Kart.Shared.Observability;
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

/// <summary>
/// api-contract.yaml /admin/permission-grants* (ADM-1, ADM-2, ADM-3) — the permission-management
/// meta-category. Issue/Revoke belong to the "Roles &amp; Permission Management (Admin)" flow
/// (business-flows.md flow #15: Create Role/Assign Permissions/Assign Role to Staff maps to Issue,
/// Update/Revoke Permission maps to Revoke); KartFlowContext.Push mirrors every sibling
/// controller's own convention. ListPermissionGrants is a read with no admin_actions audit row, so
/// — same as every other controller's own read/write split (e.g. OrdersController's own doc
/// comment) — it deliberately gets no Flow tag.
/// </summary>
[ApiController]
[Route("v1/admin/permission-grants")]
[Authorize(Policy = AuthenticationExtensions.AdminPolicy)]
public sealed class PermissionGrantsController : AdminControllerBase
{
    private const string FlowName = "RolesPermissionManagementAdmin";

    private readonly ISender _sender;
    private readonly ILogger<PermissionGrantsController> _logger;

    public PermissionGrantsController(ISender sender, ILogger<PermissionGrantsController> logger)
    {
        _sender = sender;
        _logger = logger;
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
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: issue permission grant received (principal {PrincipalId}, category {Category})", "AdminPermissionGrantsControllerReceived", request.PrincipalId, request.Category);

        var command = new IssuePermissionGrantCommand(
            ActingPrincipalId,
            request.PrincipalId,
            PermissionCategoryExtensions.ParseWireValue(request.Category),
            idempotencyKey);

        _logger.LogInformation("Stage {Stage}: dispatching IssuePermissionGrantCommand for principal {PrincipalId}, category {Category}", "IssuePermissionGrantCommandDispatched", request.PrincipalId, request.Category);
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
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: revoke permission grant {GrantId} received", "AdminPermissionGrantsControllerReceived", grantId);

        var command = new RevokePermissionGrantCommand(ActingPrincipalId, grantId, ifMatch, idempotencyKey);
        _logger.LogInformation("Stage {Stage}: dispatching RevokePermissionGrantCommand for grant {GrantId}", "RevokePermissionGrantCommandDispatched", grantId);
        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<PermissionGrantDto, PermissionGrantDto>(result, r => Ok(r));
    }
}

/// <summary>api-contract.yaml issuePermissionGrant requestBody shape.</summary>
public sealed record IssuePermissionGrantRequest(string PrincipalId, string Category);
