using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.ReorderCategory;

/// <summary>api-contract.yaml POST /admin/categories/{categoryId}/reorder (ADM-9).</summary>
public sealed record ReorderCategoryCommand(
    string ActingPrincipalId,
    string CategoryId,
    int DisplayOrder,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
