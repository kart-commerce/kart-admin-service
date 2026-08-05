using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;
using MediatR;

namespace KartAdminService.Application.Features.IssuePermissionGrant;

/// <summary>
/// ADM-1 — the foundation ticket: lands both of Admin's own tables' mechanics (tickets.md). Issuing
/// a grant is itself gated by the acting principal already holding a live `permission-management`
/// grant (ddd-model.md's self-referential rule) — the sole exception, a one-time out-of-band seed
/// script bootstrap for a deployment's very first such grant, never goes through this handler.
/// </summary>
public sealed class IssuePermissionGrantCommandHandler : IRequestHandler<IssuePermissionGrantCommand, Result<PermissionGrantDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IPermissionGrantRepository _grantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public IssuePermissionGrantCommandHandler(
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

    public async Task<Result<PermissionGrantDto>> Handle(IssuePermissionGrantCommand request, CancellationToken cancellationToken)
    {
        var context = JsonContextSerializer.Serialize(new { request.TargetPrincipalId, Category = request.Category.ToWireValue() });

        var executed = await _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.PermissionManagement,
            request.IdempotencyKey,
            ActionNames.GrantIssue,
            ct => IssueGrantAsync(request, ct),
            context,
            cancellationToken);

        if (executed.IsFailure)
        {
            return Result.Failure<PermissionGrantDto>(executed.Error);
        }

        // The audit row's EntityId is this new grant's own id (Domain Invariant #1) — re-fetch
        // so both the fresh-issue and idempotent-replay paths return the current PermissionGrant
        // shape api-contract.yaml specifies for this endpoint, not the generic AdminActionResult.
        var grant = await _grantRepository.GetByIdAsync(Guid.Parse(executed.Value.EntityId), cancellationToken);
        return grant is null
            ? Result.Failure<PermissionGrantDto>(Error.NotFound($"Grant '{executed.Value.EntityId}' not found after issue."))
            : Result.Success(PermissionGrantDto.FromDomain(grant));
    }

    private async Task<Result<string>> IssueGrantAsync(IssuePermissionGrantCommand request, CancellationToken cancellationToken)
    {
        var alreadyLive = await _grantRepository.HasLiveGrantAsync(request.TargetPrincipalId, request.Category, cancellationToken);
        if (alreadyLive)
        {
            return Result.Failure<string>(Error.Conflict(
                $"A live grant already exists for principal '{request.TargetPrincipalId}', category '{request.Category.ToWireValue()}'. Revoke it before re-issuing."));
        }

        var now = _timeProvider.GetUtcNow();
        var issueResult = AdminPermissionGrant.Issue(request.TargetPrincipalId, request.Category, request.ActingPrincipalId, now);
        if (issueResult.IsFailure)
        {
            return Result.Failure<string>(issueResult.Error);
        }

        var grant = issueResult.Value;
        await _grantRepository.AddAsync(grant, cancellationToken);
        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure
            ? Result.Failure<string>(saveResult.Error)
            : Result.Success(grant.GrantId.ToString());
    }
}
