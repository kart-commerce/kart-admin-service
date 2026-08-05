using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.UpdateCategory;

/// <summary>api-contract.yaml PUT /admin/categories/{categoryId} (ADM-8).</summary>
public sealed record UpdateCategoryCommand(
    string ActingPrincipalId,
    string CategoryId,
    CategoryWriteRequest Category,
    string IfMatch,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
