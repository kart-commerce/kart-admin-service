using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.MoveCategory;

public sealed class MoveCategoryCommandHandler : IRequestHandler<MoveCategoryCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly ICategoryServiceClient _client;

    public MoveCategoryCommandHandler(AdminActionExecutor executor, ICategoryServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(MoveCategoryCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.CatalogManagement,
            request.IdempotencyKey,
            ActionNames.CategoryMove,
            ct => _client.MoveCategoryAsync(request.CategoryId, request.NewParentId, request.IdempotencyKey.ToString(), ct)
                .WithKnownEntityId(request.CategoryId),
            JsonContextSerializer.Serialize(new { request.NewParentId }),
            cancellationToken);
}
