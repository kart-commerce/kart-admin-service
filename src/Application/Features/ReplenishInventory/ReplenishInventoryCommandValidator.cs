using FluentValidation;

namespace KartAdminService.Application.Features.ReplenishInventory;

public sealed class ReplenishInventoryCommandValidator : AbstractValidator<ReplenishInventoryCommand>
{
    public ReplenishInventoryCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.QtyAdded).GreaterThan(0);
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}
