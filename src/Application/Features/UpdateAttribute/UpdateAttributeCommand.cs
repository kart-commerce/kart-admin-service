using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.UpdateAttribute;

/// <summary>api-contract.yaml PUT /admin/attributes/{attributeId}.</summary>
public sealed record UpdateAttributeCommand(
    string ActingPrincipalId,
    string AttributeId,
    AttributeUpdateRequest Attribute,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
