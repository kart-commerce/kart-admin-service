using System.Net;
using Kart.Shared.Domain;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>
/// Shared "send, then map the outcome to a Result" mechanics for every downstream client — the
/// same three failure shapes api-contract.yaml documents for every proxied endpoint (404, 409,
/// 503 "circuit breaker open — no write happened, safe to retry with the same Idempotency-Key").
/// Extracted once five near-identical try/catch blocks would otherwise exist, one per client.
/// </summary>
public static class DownstreamCallResultMapper
{
    public static async Task<Result<TSuccess>> ExecuteAsync<TSuccess>(
        Func<Task<HttpResponseMessage>> send,
        Func<HttpResponseMessage, CancellationToken, Task<TSuccess>> onSuccess,
        string serviceName,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await send();
        }
        catch (Exception ex) when (ex is BrokenCircuitException or TimeoutRejectedException or HttpRequestException)
        {
            return Result.Failure<TSuccess>(Error.Custom(
                "downstream_unavailable",
                $"{serviceName} is unavailable — no write happened, safe to retry with the same Idempotency-Key."));
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                return Result.Success(await onSuccess(response, cancellationToken));
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => Result.Failure<TSuccess>(Error.NotFound($"{serviceName} reported the entity was not found.")),
                HttpStatusCode.Conflict => Result.Failure<TSuccess>(Error.Conflict($"{serviceName} rejected the write due to a conflicting/stale version.")),
                HttpStatusCode.ServiceUnavailable => Result.Failure<TSuccess>(Error.Custom("downstream_unavailable", $"{serviceName} is unavailable — no write happened.")),
                // A downstream 400 means this service sent a request the owning service's own
                // contract rejects (e.g. a missing required field our own validators don't yet
                // check for) — that's client-fixable, not a 500; ResultExtensions.StatusCodeFor
                // maps "validation_error" to 400 the same way a local FluentValidation failure does.
                HttpStatusCode.BadRequest => Result.Failure<TSuccess>(Error.Validation($"{serviceName} rejected the request as invalid.")),
                // A downstream 401/403 means the owning service rejected *this service's own*
                // service-principal credentials/claims — never the end user's fault, since callers
                // never reach here without already having passed AdminActionExecutor's own
                // fine-grained grant check (permission_denied) against the *caller's* grant.
                // Surfacing this distinctly (instead of falling into the generic "unexpected N
                // response" bucket below, which ResultExtensions.StatusCodeFor defaults to a bare
                // 500) is what actually lets an operator tell "our service-principal's roles/scope
                // claim or JWKS trust is misconfigured against {serviceName}" apart from every
                // other kind of downstream failure.
                HttpStatusCode.Unauthorized => Result.Failure<TSuccess>(Error.Custom(
                    "downstream_unauthorized",
                    $"{serviceName} rejected this service's own credentials as unauthenticated — check its service-principal token/signing-key trust.")),
                HttpStatusCode.Forbidden => Result.Failure<TSuccess>(Error.Custom(
                    "downstream_forbidden",
                    $"{serviceName} rejected this service's own credentials as unauthorized for this operation — check its service-principal's role/scope claims against {serviceName}'s policy.")),
                _ => Result.Failure<TSuccess>(Error.Custom("downstream_error", $"{serviceName} returned an unexpected {(int)response.StatusCode} response.")),
            };
        }
    }

    public static async Task<Result> ExecuteAsync(
        Func<Task<HttpResponseMessage>> send,
        string serviceName,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(send, static (_, _) => Task.FromResult(true), serviceName, cancellationToken);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }
}
