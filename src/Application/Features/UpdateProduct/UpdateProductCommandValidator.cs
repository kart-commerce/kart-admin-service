using FluentValidation;

namespace KartAdminService.Application.Features.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.IfMatch).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.Product.Name).NotEmpty();
        // ProductServiceClient.ToUpdateBody unconditionally sends categoryId on every PATCH -
        // an omitted/blank value here would silently overwrite the product group's category
        // (Product Service treats any field present in the PATCH body as an explicit overwrite).
        RuleFor(x => x.Product.CategoryId).NotEmpty();
    }
}
