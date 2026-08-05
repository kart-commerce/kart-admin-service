using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KartAdminService.Application.Common.Behaviours;

/// <summary>
/// requirement-spec.md's Observability NFR row: every command/query gets a structured
/// Information log on completion, tagged with its own name and duration. Deliberately never
/// logs the request/response objects themselves (only the request's type name), so this can't
/// leak PII/internals by construction. Exceptions are intentionally left unlogged here and
/// rethrown as-is: they're logged once, at the true boundary (Kart.Shared.ErrorHandling's
/// KartExceptionHandler), not duplicated at every pipeline layer.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        _logger.LogInformation(
            "{RequestName} completed in {ElapsedMilliseconds}ms",
            requestName,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}
