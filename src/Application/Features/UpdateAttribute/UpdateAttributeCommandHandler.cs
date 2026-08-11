using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.UpdateAttribute;

public sealed class UpdateAttributeCommandHandler : IRequestHandler<UpdateAttributeCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IAttributeServiceClient _client;

    public UpdateAttributeCommandHandler(AdminActionExecutor executor, IAttributeServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(UpdateAttributeCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.CatalogManagement,
            request.IdempotencyKey,
            ActionNames.AttributeUpdate,
            ct => _client.UpdateAttributeAsync(request.AttributeId, request.Attribute, request.IdempotencyKey.ToString(), ct)
                .WithKnownEntityId(request.AttributeId),
            JsonContextSerializer.Serialize(request.Attribute),
            cancellationToken);
}
