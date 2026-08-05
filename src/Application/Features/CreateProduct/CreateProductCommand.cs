using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.CreateProduct;

/// <summary>api-contract.yaml POST /admin/products (ADM-4). Category `catalog-management`.</summary>
public sealed record CreateProductCommand(
    string ActingPrincipalId,
    ProductWriteRequest Product,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
