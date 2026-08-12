using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.ResolveOrderFulfillmentException;

public sealed class ResolveOrderFulfillmentExceptionCommandHandler : IRequestHandler<ResolveOrderFulfillmentExceptionCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IOrderServiceClient _client;

    public ResolveOrderFulfillmentExceptionCommandHandler(AdminActionExecutor executor, IOrderServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(ResolveOrderFulfillmentExceptionCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.OrderManagement,
            request.IdempotencyKey,
            ActionNames.OrderFulfillmentExceptionResolve,
            ct => _client.ResolveFulfillmentExceptionAsync(request.OrderId, request.Action, request.IdempotencyKey.ToString(), ct)
                .WithKnownEntityId(request.OrderId.ToString()),
            JsonContextSerializer.Serialize(new { request.Action }),
            cancellationToken);
}
