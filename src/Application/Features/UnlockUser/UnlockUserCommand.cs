using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.UnlockUser;

/// <summary>api-contract.yaml POST /admin/users/{userId}/unlock (ADM-14).</summary>
public sealed record UnlockUserCommand(
    string ActingPrincipalId,
    string UserId,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
