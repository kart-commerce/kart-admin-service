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
