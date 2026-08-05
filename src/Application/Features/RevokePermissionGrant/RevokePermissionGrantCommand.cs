using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.RevokePermissionGrant;

/// <summary>api-contract.yaml POST /admin/permission-grants/{grantId}/revoke (ADM-2). ExpectedVersion is the caller-supplied If-Match precondition (design-decisions.md, "Concurrency Control for Back-Office Writes").</summary>
public sealed record RevokePermissionGrantCommand(
    string ActingPrincipalId,
    Guid GrantId,
    int ExpectedVersion,
    Guid IdempotencyKey) : IRequest<Result<PermissionGrantDto>>;
