using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.UpdateProduct;

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IProductServiceClient _client;

    public UpdateProductCommandHandler(AdminActionExecutor executor, IProductServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.CatalogManagement,
            request.IdempotencyKey,
            ActionNames.ProductUpdate,
            ct => UpdateAsync(request, ct),
            JsonContextSerializer.Serialize(request.Product),
            cancellationToken);

    // request.ProductId is the SKU (the /admin/products/{sku} contract); Product Service's own
    // PATCH /v1/product-groups/{id} is GUID-keyed, so the group id must be resolved first.
    private async Task<Result<string>> UpdateAsync(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var lookup = await _client.GetProductGroupIdAsync(request.ProductId, cancellationToken);
        if (lookup.IsFailure)
        {
            return Result.Failure<string>(lookup.Error);
        }

        var groupUpdate = await _client.UpdateProductAsync(lookup.Value, request.Product, request.IfMatch, request.IdempotencyKey.ToString(), cancellationToken);
        if (groupUpdate.IsFailure)
        {
            return Result.Failure<string>(groupUpdate.Error);
        }

        // Price lives on the Variant (SKU-keyed), not the group PATCH just above - a separate
        // call, or admin-web's price field silently no-ops (the real bug this fixes: the form
        // "saved" successfully while the old price stayed live everywhere downstream).
        if (request.Product.Price is not null)
        {
            var priceUpdate = await _client.UpdatePriceAsync(request.ProductId, request.Product.Price, request.IdempotencyKey.ToString(), cancellationToken);
            if (priceUpdate.IsFailure)
            {
                return Result.Failure<string>(priceUpdate.Error);
            }
        }

        return Result.Success(request.ProductId);
    }
}
