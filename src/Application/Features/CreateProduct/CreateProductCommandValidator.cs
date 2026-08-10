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
        // Product Service's own contract requires `sku` on create (POST /v1/product-groups) -
        // this was previously unchecked here, so a missing SKU passed Admin's own validation and
        // only failed downstream, at Product Service.
        RuleFor(x => x.Product.Sku).NotEmpty();
    }
}
