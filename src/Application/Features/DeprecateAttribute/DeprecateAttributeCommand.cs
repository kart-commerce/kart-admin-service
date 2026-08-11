using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.DeprecateAttribute;

/// <summary>api-contract.yaml DELETE /admin/attributes/{attributeId}.</summary>
public sealed record DeprecateAttributeCommand(
    string ActingPrincipalId,
    string AttributeId,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
