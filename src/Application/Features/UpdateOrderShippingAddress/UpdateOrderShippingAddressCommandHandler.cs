using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.UpdateOrderShippingAddress;

public sealed class UpdateOrderShippingAddressCommandHandler : IRequestHandler<UpdateOrderShippingAddressCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IOrderServiceClient _client;

    public UpdateOrderShippingAddressCommandHandler(AdminActionExecutor executor, IOrderServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(UpdateOrderShippingAddressCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.OrderManagement,
            request.IdempotencyKey,
            ActionNames.OrderShippingAddressUpdate,
            ct => _client.UpdateShippingAddressAsync(request.OrderId, request.Address, request.IdempotencyKey.ToString(), ct)
                .WithKnownEntityId(request.OrderId.ToString()),
            JsonContextSerializer.Serialize(request.Address),
            cancellationToken);
}
