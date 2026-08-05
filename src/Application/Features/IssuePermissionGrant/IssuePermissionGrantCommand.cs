using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.IssuePermissionGrant;

/// <summary>api-contract.yaml POST /admin/permission-grants (ADM-1). ActingPrincipalId is the caller (from the validated JWT), resolved by the controller — never client-suppliable.</summary>
public sealed record IssuePermissionGrantCommand(
    string ActingPrincipalId,
    string TargetPrincipalId,
    PermissionCategory Category,
    Guid IdempotencyKey) : IRequest<Result<PermissionGrantDto>>;
