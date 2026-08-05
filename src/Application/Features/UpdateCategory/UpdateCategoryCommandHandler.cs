using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.UpdateCategory;

public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly ICategoryServiceClient _client;

    public UpdateCategoryCommandHandler(AdminActionExecutor executor, ICategoryServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.CatalogManagement,
            request.IdempotencyKey,
            ActionNames.CategoryUpdate,
            ct => _client.UpdateCategoryAsync(request.CategoryId, request.Category, request.IfMatch, request.IdempotencyKey.ToString(), ct)
                .WithKnownEntityId(request.CategoryId),
            JsonContextSerializer.Serialize(request.Category),
            cancellationToken);
}
