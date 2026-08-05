using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.RevokePermissionGrant;

/// <summary>
/// ADM-2. Revocation takes effect on the affected principal's very next request — no token-TTL
/// exposure window (edge-cases.md, "Stale Admin Permission Outliving an Identity-Side Revocation")
/// — because every /admin/* handler reads admin_permission_grants live, uncached.
/// </summary>
public sealed class RevokePermissionGrantCommandHandler : IRequestHandler<RevokePermissionGrantCommand, Result<PermissionGrantDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IPermissionGrantRepository _grantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public RevokePermissionGrantCommandHandler(
        AdminActionExecutor executor,
        IPermissionGrantRepository grantRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _executor = executor;
        _grantRepository = grantRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PermissionGrantDto>> Handle(RevokePermissionGrantCommand request, CancellationToken cancellationToken)
    {
        var context = JsonContextSerializer.Serialize(new { request.GrantId, request.ExpectedVersion });

        var executed = await _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.PermissionManagement,
            request.IdempotencyKey,
            ActionNames.GrantRevoke,
            ct => RevokeGrantAsync(request, ct),
            context,
            cancellationToken);

        if (executed.IsFailure)
        {
            return Result.Failure<PermissionGrantDto>(executed.Error);
        }

        var grant = await _grantRepository.GetByIdAsync(Guid.Parse(executed.Value.EntityId), cancellationToken);
        return grant is null
            ? Result.Failure<PermissionGrantDto>(Error.NotFound($"Grant '{executed.Value.EntityId}' not found after revoke."))
            : Result.Success(PermissionGrantDto.FromDomain(grant));
    }

    private async Task<Result<string>> RevokeGrantAsync(RevokePermissionGrantCommand request, CancellationToken cancellationToken)
    {
        var grant = await _grantRepository.GetByIdAsync(request.GrantId, cancellationToken);
        if (grant is null)
        {
            return Result.Failure<string>(Error.NotFound($"Grant '{request.GrantId}' not found."));
        }

        if (grant.Version != request.ExpectedVersion)
        {
            return Result.Failure<string>(Error.Conflict(
                $"Grant '{request.GrantId}' has been modified since version {request.ExpectedVersion} was read (current version {grant.Version}). Re-read and retry."));
        }

        var now = _timeProvider.GetUtcNow();
        var revokeResult = grant.Revoke(request.ActingPrincipalId, now);
        if (revokeResult.IsFailure)
        {
            return Result.Failure<string>(revokeResult.Error);
        }

        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure
            ? Result.Failure<string>(saveResult.Error)
            : Result.Success(grant.GrantId.ToString());
    }
}
