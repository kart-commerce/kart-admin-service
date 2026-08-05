using Kart.Shared.Domain;
using MediatR;
using KartAdminService.Application.Common.Models;

namespace KartAdminService.Application.Features.LockUser;

/// <summary>api-contract.yaml POST /admin/users/{userId}/lock (ADM-13). Category `user-suspension`.</summary>
public sealed record LockUserCommand(
    string ActingPrincipalId,
    string UserId,
    string? Reason,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
