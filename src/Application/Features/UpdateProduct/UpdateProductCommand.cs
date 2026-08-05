using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.UpdateProduct;

/// <summary>api-contract.yaml PUT /admin/products/{productId} (ADM-5). IfMatch is forwarded to Product Service, which owns this record's own version (design-decisions.md).</summary>
public sealed record UpdateProductCommand(
    string ActingPrincipalId,
    string ProductId,
    ProductWriteRequest Product,
    string IfMatch,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
