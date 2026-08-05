using Kart.Shared.Domain;
using Kart.Shared.ErrorHandling;
using Microsoft.AspNetCore.Mvc;

namespace KartAdminService.Api.Common;

/// <summary>
/// Translates a Handler's Result&lt;T&gt; failure (api-standards.md: "Domain/business errors use
/// a Result/Either pattern - not exceptions") into the HTTP status + Kart.Shared.ErrorHandling's
/// ProblemDetails envelope api-contract.yaml specifies per endpoint — the one platform-wide
/// error shape, produced the same way whether the failure came from a Result or from
/// KartExceptionHandler catching a genuine unhandled exception (design-decisions.md, "Global
/// Exception Handling & Consistent Response Model").
/// </summary>
public static class ResultExtensions
{
    public static ActionResult<TResponse> ToActionResult<TValue, TResponse>(
        this ControllerBase controller,
        Result<TValue> result,
        Func<TValue, ActionResult<TResponse>> onSuccess)
    {
        return result.IsSuccess ? onSuccess(result.Value) : controller.MapFailure<TResponse>(result.Error);
    }

    public static ActionResult MapFailure(this ControllerBase controller, Error error)
    {
        var statusCode = StatusCodeFor(error.Code);
        var problem = KartProblemDetailsFactory.Create(controller.HttpContext, statusCode, error.Code, error.Message);
        return controller.StatusCode(statusCode, problem);
    }

    private static ActionResult<TResponse> MapFailure<TResponse>(this ControllerBase controller, Error error) =>
        new(controller.MapFailure(error));

    private static int StatusCodeFor(string errorCode) => errorCode switch
    {
        "not_found" => StatusCodes.Status404NotFound,
        "permission_denied" => StatusCodes.Status403Forbidden,
        "conflict" => StatusCodes.Status409Conflict,
        "downstream_unavailable" => StatusCodes.Status503ServiceUnavailable,
        "validation_error" => StatusCodes.Status400BadRequest,
        "unauthorized" => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status500InternalServerError,
    };
}
