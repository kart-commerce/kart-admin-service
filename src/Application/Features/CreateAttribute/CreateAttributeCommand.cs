using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.CreateAttribute;

/// <summary>api-contract.yaml POST /admin/attributes.</summary>
public sealed record CreateAttributeCommand(
    string ActingPrincipalId,
    AttributeWriteRequest Attribute,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
