using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.ProvisionWarehouseStock;

public sealed class ProvisionWarehouseStockCommandHandler : IRequestHandler<ProvisionWarehouseStockCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IInventoryServiceClient _client;

    public ProvisionWarehouseStockCommandHandler(AdminActionExecutor executor, IInventoryServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(ProvisionWarehouseStockCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.InventoryReplenishment,
            request.IdempotencyKey,
            ActionNames.InventoryProvision,
            ct => _client.ProvisionAsync(request.WarehouseId, request.Sku, request.InitialQty, request.ReplenishmentThreshold, request.TargetStockingLevel, ct)
                .WithKnownEntityId($"{request.WarehouseId}:{request.Sku}"),
            JsonContextSerializer.Serialize(new { request.WarehouseId, request.Sku, request.InitialQty, request.ReplenishmentThreshold, request.TargetStockingLevel }),
            cancellationToken);
}
