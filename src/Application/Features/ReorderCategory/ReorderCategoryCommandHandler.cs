using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.ReorderCategory;

public sealed class ReorderCategoryCommandHandler : IRequestHandler<ReorderCategoryCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly ICategoryServiceClient _client;

    public ReorderCategoryCommandHandler(AdminActionExecutor executor, ICategoryServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(ReorderCategoryCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.CatalogManagement,
            request.IdempotencyKey,
            ActionNames.CategoryReorder,
            ct => _client.ReorderCategoryAsync(request.CategoryId, request.DisplayOrder, request.IdempotencyKey.ToString(), ct)
                .WithKnownEntityId(request.CategoryId),
            JsonContextSerializer.Serialize(new { request.DisplayOrder }),
            cancellationToken);
}
