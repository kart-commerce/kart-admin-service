using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.DeactivateProduct;

public sealed class DeactivateProductCommandHandler : IRequestHandler<DeactivateProductCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IProductServiceClient _client;

    public DeactivateProductCommandHandler(AdminActionExecutor executor, IProductServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(DeactivateProductCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.CatalogManagement,
            request.IdempotencyKey,
            ActionNames.ProductDeactivate,
            ct => DeactivateAsync(request, ct),
            context: null,
            cancellationToken);

    // request.ProductId is the SKU (the /admin/products/{sku} contract); Product Service's own
    // PATCH /v1/product-groups/{id} is GUID-keyed, so the group id must be resolved first.
    private async Task<Result<string>> DeactivateAsync(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        var lookup = await _client.GetProductGroupIdAsync(request.ProductId, cancellationToken);
        if (lookup.IsFailure)
        {
            return Result.Failure<string>(lookup.Error);
        }

        return await _client.DeactivateProductAsync(lookup.Value, request.IdempotencyKey.ToString(), cancellationToken)
            .WithKnownEntityId(request.ProductId);
    }
}
