using FluentValidation;

namespace KartAdminService.Application.Features.AdminUpdateOrderStatus;

public sealed class AdminUpdateOrderStatusCommandValidator : AbstractValidator<AdminUpdateOrderStatusCommand>
{
    // Order Service itself is the source of truth restricting TargetStatus to
    // {Shipped, Delivered, FulfillmentException} and re-validating the legal-transition graph —
    // this validator only checks shape, never duplicates that domain policy, so the two services
    // can't silently drift on which targets are actually allowed.
    public AdminUpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.TargetStatus).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}
