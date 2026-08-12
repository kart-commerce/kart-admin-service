using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.CreateAttribute;

public sealed class CreateAttributeCommandHandler : IRequestHandler<CreateAttributeCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IAttributeServiceClient _client;

    public CreateAttributeCommandHandler(AdminActionExecutor executor, IAttributeServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(CreateAttributeCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.CatalogManagement,
            request.IdempotencyKey,
            ActionNames.AttributeCreate,
            ct => _client.CreateAttributeAsync(request.Attribute, request.IdempotencyKey.ToString(), ct),
            JsonContextSerializer.Serialize(request.Attribute),
            cancellationToken);
}
