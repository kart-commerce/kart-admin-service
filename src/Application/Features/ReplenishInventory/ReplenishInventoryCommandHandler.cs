using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.ReplenishInventory;

public sealed class ReplenishInventoryCommandHandler : IRequestHandler<ReplenishInventoryCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IInventoryServiceClient _client;

    public ReplenishInventoryCommandHandler(AdminActionExecutor executor, IInventoryServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(ReplenishInventoryCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.InventoryReplenishment,
            request.IdempotencyKey,
            ActionNames.InventoryReplenish,
            ct => _client.ReplenishAsync(request.Sku, request.WarehouseId, request.QtyAdded, request.Reason, request.IdempotencyKey.ToString(), ct)
                .WithKnownEntityId(request.Sku),
            JsonContextSerializer.Serialize(new { request.WarehouseId, request.QtyAdded, request.Reason }),
            cancellationToken);
}
