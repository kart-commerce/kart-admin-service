using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.CreateCoupon;

/// <summary>api-contract.yaml POST /admin/coupons (ADM-11). Category `coupon-issuance`.</summary>
public sealed record CreateCouponCommand(
    string ActingPrincipalId,
    CouponWriteRequest Coupon,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
