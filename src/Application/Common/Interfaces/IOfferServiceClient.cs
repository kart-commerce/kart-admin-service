using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;

namespace KartAdminService.Application.Common.Interfaces;

/// <summary>Adapter wrapping Offer Service's admin-only Coupon write API (architecture.md Dependencies table; ADR-0001 — Offer owns the Coupon aggregate).</summary>
public interface IOfferServiceClient
{
    Task<Result> CreateCouponAsync(CouponWriteRequest request, string idempotencyKey, CancellationToken cancellationToken);

    Task<Result> DeactivateCouponAsync(string couponCode, string idempotencyKey, CancellationToken cancellationToken);
}
