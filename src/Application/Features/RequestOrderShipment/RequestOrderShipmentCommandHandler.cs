using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.RequestOrderShipment;

public sealed class RequestOrderShipmentCommandHandler : IRequestHandler<RequestOrderShipmentCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IOrderServiceClient _client;

    public RequestOrderShipmentCommandHandler(AdminActionExecutor executor, IOrderServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(RequestOrderShipmentCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.OrderManagement,
            request.IdempotencyKey,
            ActionNames.OrderShipmentRequest,
            ct => _client.RequestShipmentAsync(request.OrderId, request.IdempotencyKey.ToString(), ct)
                .WithKnownEntityId(request.OrderId.ToString()),
            context: null,
            cancellationToken);
}
