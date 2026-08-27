using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.ReconcileStock;

public sealed class ReconcileStockCommandHandler : IRequestHandler<ReconcileStockCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IInventoryServiceClient _client;

    public ReconcileStockCommandHandler(AdminActionExecutor executor, IInventoryServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(ReconcileStockCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.InventoryReplenishment,
            request.IdempotencyKey,
            ActionNames.InventoryReconcile,
            ct => _client.ReconcileAsync(request.WarehouseId, request.Sku, request.CountedQty, request.Reason, ct)
                .WithKnownEntityId($"{request.WarehouseId}:{request.Sku}"),
            JsonContextSerializer.Serialize(new { request.WarehouseId, request.Sku, request.CountedQty, request.Reason }),
            cancellationToken);
}
