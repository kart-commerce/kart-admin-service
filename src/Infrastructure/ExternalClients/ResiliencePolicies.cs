using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>
/// design-decisions.md, "Resilience Pattern for Outbound Calls to Owning Services": a short
/// per-call timeout carved from the 300ms P95 write budget, a small bounded retry (safe only
/// because every mutating call forwards the Idempotency-Key/If-Match headers — "Idempotency
/// Mechanism for Outbound Write Calls"), and one independent circuit breaker per downstream
/// owning service, never one shared breaker (so e.g. a Category Service outage blocks only
/// catalog-management actions, not coupon-issuance or user-suspension). Each call to
/// <see cref="BuildPolicy"/> returns a fresh breaker instance — <c>AddHttpClient&lt;TInterface,
/// TImplementation&gt;()</c> registers one such policy per named client, so the five downstream
/// clients each get their own independent breaker state, never a shared one.
/// </summary>
public static class ResiliencePolicies
{
    public static IAsyncPolicy<HttpResponseMessage> BuildPolicy(TimeSpan timeout)
    {
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(timeout, TimeoutStrategy.Optimistic);

        // Bounded retry (2 attempts, short fixed backoff) on transient faults and 5xx/408 —
        // safe only because every mutating call forwards Idempotency-Key (and If-Match where
        // applicable), so a retried attempt against the same owning service is dedupe-safe.
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(50 * attempt));

        // Independent circuit breaker per owning service: after 5 consecutive faults, stop
        // sending requests to that one peer for 15s and fail fast (503) — a single slow/down
        // dependency must not silently blow Admin's own write-path P95 or cascade into
        // unrelated categories (architecture.md, "Distributed-Monolith Risk").
        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(15));

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
    }

    public static bool IsCircuitBreakerOrTimeout(Exception exception) =>
        exception is BrokenCircuitException or TimeoutRejectedException;
}
