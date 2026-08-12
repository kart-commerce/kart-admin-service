using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;

namespace KartAdminService.Application.Common.Interfaces;

/// <summary>
/// Adapter wrapping Category Service's own Attribute write API - Attribute is a second aggregate
/// in that same service (see AttributeServiceClient's doc comment), not a separate microservice,
/// so this client shares Category's downstream endpoint config rather than needing its own.
/// </summary>
public interface IAttributeServiceClient
{
    Task<Result<string>> CreateAttributeAsync(AttributeWriteRequest request, string idempotencyKey, CancellationToken cancellationToken);

    Task<Result> UpdateAttributeAsync(string attributeId, AttributeUpdateRequest request, string idempotencyKey, CancellationToken cancellationToken);

    Task<Result> DeprecateAttributeAsync(string attributeId, string idempotencyKey, CancellationToken cancellationToken);
}
