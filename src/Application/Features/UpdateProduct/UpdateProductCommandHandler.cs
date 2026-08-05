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
            ct => _client.UpdateProductAsync(request.ProductId, request.Product, request.IfMatch, request.IdempotencyKey.ToString(), ct)
                .WithKnownEntityId(request.ProductId),
            JsonContextSerializer.Serialize(request.Product),
            cancellationToken);
}
