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
///
/// checkpoint-logging-standard.md's stage 3 ("<Command>HandlerStarted", first line inside
/// Handle()) is generalized here rather than duplicated in every handler, exactly like
/// kart-identity-service's own LoggingBehaviour reference implementation — this behavior already
/// wraps every MediatR request platform-wide, so it's the one place that's true by construction
/// instead of by every handler author remembering to add it.
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

        _logger.LogInformation(
            "Stage {Stage}: {RequestName} handler started",
            $"{requestName}HandlerStarted",
            requestName);

        var response = await next();

        _logger.LogInformation(
            "Stage {Stage}: {RequestName} completed in {ElapsedMilliseconds}ms",
            $"{requestName}Completed",
            requestName,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}
