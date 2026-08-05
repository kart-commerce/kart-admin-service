using FluentValidation;

namespace KartAdminService.Application.Features.IssuePermissionGrant;

public sealed class IssuePermissionGrantCommandValidator : AbstractValidator<IssuePermissionGrantCommand>
{
    public IssuePermissionGrantCommandValidator()
    {
        RuleFor(x => x.TargetPrincipalId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.Category).IsInEnum();
    }
}
