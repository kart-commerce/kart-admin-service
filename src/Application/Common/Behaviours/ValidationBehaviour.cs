using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KartAdminService.Application.Common.Behaviours;

/// <summary>
/// Runs every registered FluentValidation validator for a request before its Handler executes
/// (api-standards.md: "Input validated at the API boundary"). A request type with no registered
/// validator passes through untouched - no empty ceremonial Validator.cs is required per slice.
///
/// checkpoint-logging-standard.md's stage 4 ("<Rule>ValidationFailed", logged at Warning with the
/// reason before throwing) is generalized here for every FluentValidation validator on the
/// platform, rather than duplicated per handler — mirrors kart-identity-service's own
/// ValidationBehaviour reference implementation exactly. The ValidationException itself is still
/// logged once more, generically, at the API boundary by KartExceptionHandler; this line is the
/// one that's greppable by Stage and carries the actual field-level reasons.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehaviour<TRequest, TResponse>> _logger;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidationBehaviour<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToList();

            if (failures.Count > 0)
            {
                var requestName = typeof(TRequest).Name;

                _logger.LogWarning(
                    "Stage {Stage}: {RequestName} rejected — {Errors}",
                    $"{requestName}ValidationFailed",
                    requestName,
                    string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}")));

                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
