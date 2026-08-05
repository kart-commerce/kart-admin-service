using FluentValidation;

namespace KartAdminService.Application.Features.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.Product.Name).NotEmpty();
        RuleFor(x => x.Product.CategoryId).NotEmpty();
        RuleFor(x => x.Product.Price.Amount).GreaterThan(0);
        RuleFor(x => x.Product.Price.Currency).NotEmpty();
    }
}
