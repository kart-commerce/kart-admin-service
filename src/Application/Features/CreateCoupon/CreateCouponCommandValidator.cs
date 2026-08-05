using FluentValidation;

namespace KartAdminService.Application.Features.CreateCoupon;

public sealed class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.Coupon.CouponCode).NotEmpty();
        RuleFor(x => x.Coupon.DiscountValue.Amount).GreaterThan(0);
        RuleFor(x => x.Coupon.DiscountValue.Currency).NotEmpty();
    }
}
