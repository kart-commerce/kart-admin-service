using FluentValidation;

namespace KartAdminService.Application.Features.UpdateOrderShippingAddress;

public sealed class UpdateOrderShippingAddressCommandValidator : AbstractValidator<UpdateOrderShippingAddressCommand>
{
    public UpdateOrderShippingAddressCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.Address).NotNull();
        When(x => x.Address is not null, () =>
        {
            RuleFor(x => x.Address.RecipientName).NotEmpty();
            RuleFor(x => x.Address.Line1).NotEmpty();
            RuleFor(x => x.Address.City).NotEmpty();
            RuleFor(x => x.Address.State).NotEmpty();
            RuleFor(x => x.Address.PostalCode).NotEmpty();
            RuleFor(x => x.Address.Country).NotEmpty();
        });
    }
}
