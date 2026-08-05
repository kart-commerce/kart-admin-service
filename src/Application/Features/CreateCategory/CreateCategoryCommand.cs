using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.CreateCategory;

/// <summary>api-contract.yaml POST /admin/categories (ADM-7).</summary>
public sealed record CreateCategoryCommand(
    string ActingPrincipalId,
    CategoryWriteRequest Category,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
