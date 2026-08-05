using Kart.Shared.Domain;

namespace KartAdminService.Application.Common.Interfaces;

/// <summary>
/// Adapter wrapping kart-identity-service's own confirmed, real internal routes
/// (POST /v1/internal/users/{userId}/lock|unlock — Kart.Identity.Api.Endpoints.InternalUserEndpoints,
/// gated by the `scopes: admin` claim per ADR-0010) — unlike the other four downstream clients,
/// this one is written against a live, already-implemented contract.
/// </summary>
public interface IIdentityAdminClient
{
    Task<Result> LockUserAsync(string userId, string? reason, string idempotencyKey, CancellationToken cancellationToken);

    Task<Result> UnlockUserAsync(string userId, string idempotencyKey, CancellationToken cancellationToken);
}
