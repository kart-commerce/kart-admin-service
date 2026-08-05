using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.LockUser;

public sealed class LockUserCommandHandler : IRequestHandler<LockUserCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IIdentityAdminClient _client;

    public LockUserCommandHandler(AdminActionExecutor executor, IIdentityAdminClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(LockUserCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.UserSuspension,
            request.IdempotencyKey,
            ActionNames.UserLock,
            ct => _client.LockUserAsync(request.UserId, request.Reason, request.IdempotencyKey.ToString(), ct).WithKnownEntityId(request.UserId),
            request.Reason is null ? null : JsonContextSerializer.Serialize(new { request.Reason }),
            cancellationToken);
}
