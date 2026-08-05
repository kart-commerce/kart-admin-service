using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.MoveCategory;

/// <summary>api-contract.yaml POST /admin/categories/{categoryId}/move (ADM-10). NewParentId null moves the node to become a root category.</summary>
public sealed record MoveCategoryCommand(
    string ActingPrincipalId,
    string CategoryId,
    string? NewParentId,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
