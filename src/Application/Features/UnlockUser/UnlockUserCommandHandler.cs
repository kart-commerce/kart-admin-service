using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.UnlockUser;

public sealed class UnlockUserCommandHandler : IRequestHandler<UnlockUserCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IIdentityAdminClient _client;

    public UnlockUserCommandHandler(AdminActionExecutor executor, IIdentityAdminClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(UnlockUserCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.UserSuspension,
            request.IdempotencyKey,
            ActionNames.UserUnlock,
            ct => _client.UnlockUserAsync(request.UserId, request.IdempotencyKey.ToString(), ct).WithKnownEntityId(request.UserId),
            context: null,
            cancellationToken);
}
