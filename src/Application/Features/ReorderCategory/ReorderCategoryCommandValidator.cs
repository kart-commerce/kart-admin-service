using FluentValidation;

namespace KartAdminService.Application.Features.ReorderCategory;

public sealed class ReorderCategoryCommandValidator : AbstractValidator<ReorderCategoryCommand>
{
    public ReorderCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}
