using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.DeactivateProduct;

/// <summary>api-contract.yaml POST /admin/products/{productId}/deactivate (ADM-6).</summary>
public sealed record DeactivateProductCommand(
    string ActingPrincipalId,
    string ProductId,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
