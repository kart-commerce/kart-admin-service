using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.DeactivateCoupon;

public sealed class DeactivateCouponCommandHandler : IRequestHandler<DeactivateCouponCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IOfferServiceClient _client;

    public DeactivateCouponCommandHandler(AdminActionExecutor executor, IOfferServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(DeactivateCouponCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.CouponIssuance,
            request.IdempotencyKey,
            ActionNames.CouponDeactivate,
            ct => _client.DeactivateCouponAsync(request.CouponCode, request.IdempotencyKey.ToString(), ct).WithKnownEntityId(request.CouponCode),
            context: null,
            cancellationToken);
}
