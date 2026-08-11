using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.DeprecateAttribute;

public sealed class DeprecateAttributeCommandHandler : IRequestHandler<DeprecateAttributeCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IAttributeServiceClient _client;

    public DeprecateAttributeCommandHandler(AdminActionExecutor executor, IAttributeServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(DeprecateAttributeCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.CatalogManagement,
            request.IdempotencyKey,
            ActionNames.AttributeDeprecate,
            ct => _client.DeprecateAttributeAsync(request.AttributeId, request.IdempotencyKey.ToString(), ct)
                .WithKnownEntityId(request.AttributeId),
            context: null,
            cancellationToken);
}
