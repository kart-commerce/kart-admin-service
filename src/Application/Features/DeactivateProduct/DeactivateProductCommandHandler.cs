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
            ct => _client.DeactivateProductAsync(request.ProductId, request.IdempotencyKey.ToString(), ct).WithKnownEntityId(request.ProductId),
            context: null,
            cancellationToken);
}
