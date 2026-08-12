using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.AdminUpdateOrderStatus;

public sealed class AdminUpdateOrderStatusCommandHandler : IRequestHandler<AdminUpdateOrderStatusCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IOrderServiceClient _client;

    public AdminUpdateOrderStatusCommandHandler(AdminActionExecutor executor, IOrderServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(AdminUpdateOrderStatusCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.OrderManagement,
            request.IdempotencyKey,
            ActionNames.OrderStatusUpdate,
            ct => _client.UpdateStatusAsync(request.OrderId, request.TargetStatus, request.Reason, request.IdempotencyKey.ToString(), ct)
                .WithKnownEntityId(request.OrderId.ToString()),
            JsonContextSerializer.Serialize(new { request.TargetStatus, request.Reason }),
            cancellationToken);
}
