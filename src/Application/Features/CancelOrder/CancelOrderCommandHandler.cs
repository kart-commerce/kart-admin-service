using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.CancelOrder;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IOrderServiceClient _client;

    public CancelOrderCommandHandler(AdminActionExecutor executor, IOrderServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(CancelOrderCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.OrderManagement,
            request.IdempotencyKey,
            ActionNames.OrderCancel,
            ct => _client.CancelOrderAsync(request.OrderId, request.Reason, request.IdempotencyKey.ToString(), ct)
                .WithKnownEntityId(request.OrderId.ToString()),
            JsonContextSerializer.Serialize(new { request.Reason }),
            cancellationToken);
}
