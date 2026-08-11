using FluentValidation;

namespace KartAdminService.Application.Features.RequestOrderShipment;

public sealed class RequestOrderShipmentCommandValidator : AbstractValidator<RequestOrderShipmentCommand>
{
    public RequestOrderShipmentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}
