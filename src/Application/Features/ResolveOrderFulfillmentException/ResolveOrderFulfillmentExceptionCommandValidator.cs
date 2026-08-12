using FluentValidation;

namespace KartAdminService.Application.Features.ResolveOrderFulfillmentException;

public sealed class ResolveOrderFulfillmentExceptionCommandValidator : AbstractValidator<ResolveOrderFulfillmentExceptionCommand>
{
    public ResolveOrderFulfillmentExceptionCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.Action).NotEmpty().Must(a => a is "retry" or "cancel")
            .WithMessage("Action must be 'retry' or 'cancel'.");
    }
}
