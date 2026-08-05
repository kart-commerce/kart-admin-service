using KartAdminService.Api.Common;
using KartAdminService.Api.Security;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.CreateCoupon;
using KartAdminService.Application.Features.DeactivateCoupon;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KartAdminService.Api.Controllers;

/// <summary>api-contract.yaml /admin/coupons* (ADM-11, ADM-12) — coupon-issuance category, proxies Offer Service's admin-only write API.</summary>
[ApiController]
[Route("v1/admin/coupons")]
[Authorize(Policy = AuthenticationExtensions.AdminPolicy)]
public sealed class CouponsController : AdminControllerBase
{
    private readonly ISender _sender;

    public CouponsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> CreateCoupon(
        [FromBody] CouponWriteRequest coupon,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateCouponCommand(ActingPrincipalId, coupon, idempotencyKey), cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Created(string.Empty, r));
    }

    [HttpPost("{couponCode}/deactivate")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AdminActionResultDto>> DeactivateCoupon(
        [FromRoute] string couponCode,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateCouponCommand(ActingPrincipalId, couponCode, idempotencyKey), cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }
}
