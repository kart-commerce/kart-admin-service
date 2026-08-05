using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.DeactivateCoupon;

/// <summary>api-contract.yaml POST /admin/coupons/{couponCode}/deactivate (ADM-12).</summary>
public sealed record DeactivateCouponCommand(
    string ActingPrincipalId,
    string CouponCode,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
