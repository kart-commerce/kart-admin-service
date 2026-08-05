using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.CreateProduct;

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IProductServiceClient _client;

    public CreateProductCommandHandler(AdminActionExecutor executor, IProductServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.CatalogManagement,
            request.IdempotencyKey,
            ActionNames.ProductCreate,
            ct => _client.CreateProductAsync(request.Product, request.IdempotencyKey.ToString(), ct),
            JsonContextSerializer.Serialize(request.Product),
            cancellationToken);
}
