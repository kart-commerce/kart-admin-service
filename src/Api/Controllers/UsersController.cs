using KartAdminService.Api.Common;
using KartAdminService.Api.Security;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.LockUser;
using KartAdminService.Application.Features.UnlockUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KartAdminService.Api.Controllers;

/// <summary>api-contract.yaml /admin/users/{userId}/lock|unlock (ADM-13, ADM-14) — user-suspension category, proxies Identity Service's real internal routes.</summary>
[ApiController]
[Route("v1/admin/users")]
[Authorize(Policy = AuthenticationExtensions.AdminPolicy)]
public sealed class UsersController : AdminControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("{userId}/lock")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> LockUser(
        [FromRoute] string userId,
        [FromBody] LockUserRequest? request,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new LockUserCommand(ActingPrincipalId, userId, request?.Reason, idempotencyKey), cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }

    [HttpPost("{userId}/unlock")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> UnlockUser(
        [FromRoute] string userId,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UnlockUserCommand(ActingPrincipalId, userId, idempotencyKey), cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }
}

/// <summary>api-contract.yaml lockUser requestBody shape — optional free-text reason, stored in admin_actions.context.</summary>
public sealed record LockUserRequest(string? Reason);
