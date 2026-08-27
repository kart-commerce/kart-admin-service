using Kart.Shared.Observability;
using KartAdminService.Api.Common;
using KartAdminService.Api.Security;
using KartAdminService.Application.Common.Models;
using KartAdminService.Application.Features.CreateCoupon;
using KartAdminService.Application.Features.DeactivateCoupon;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KartAdminService.Api.Controllers;

/// <summary>
/// api-contract.yaml /admin/coupons* (ADM-11, ADM-12) — coupon-issuance category, proxies Offer
/// Service's admin-only write API. Every action here belongs to the "Offers, Coupons &amp;
/// Promotions Management (Admin)" flow (business-flows.md flow #12).
/// </summary>
[ApiController]
[Route("v1/admin/coupons")]
[Authorize(Policy = AuthenticationExtensions.AdminPolicy)]
public sealed class CouponsController : AdminControllerBase
{
    private const string FlowName = "OffersCouponsPromotionsManagementAdmin";

    private readonly ISender _sender;
    private readonly ILogger<CouponsController> _logger;

    public CouponsController(ISender sender, ILogger<CouponsController> logger)
    {
        _sender = sender;
        _logger = logger;
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
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: create coupon received for code {CouponCode}", "AdminCouponsControllerReceived", coupon.CouponCode);

        var command = new CreateCouponCommand(ActingPrincipalId, coupon, idempotencyKey);
        var result = await _sender.Send(command, cancellationToken);
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
        using var flowScope = KartFlowContext.Push(FlowName);
        _logger.LogInformation("Stage {Stage}: deactivate coupon {CouponCode} received", "AdminCouponsControllerReceived", couponCode);

        var command = new DeactivateCouponCommand(ActingPrincipalId, couponCode, idempotencyKey);
        var result = await _sender.Send(command, cancellationToken);
        return this.ToActionResult<AdminActionResultDto, AdminActionResultDto>(result, r => Ok(r));
    }
}
