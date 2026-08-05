using Kart.Shared.Domain;
using KartAdminService.Application.Common;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;
using KartAdminService.Domain.Common;
using MediatR;

namespace KartAdminService.Application.Features.CreateCoupon;

public sealed class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, Result<AdminActionResultDto>>
{
    private readonly AdminActionExecutor _executor;
    private readonly IOfferServiceClient _client;

    public CreateCouponCommandHandler(AdminActionExecutor executor, IOfferServiceClient client)
    {
        _executor = executor;
        _client = client;
    }

    public Task<Result<AdminActionResultDto>> Handle(CreateCouponCommand request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(
            request.ActingPrincipalId,
            PermissionCategory.CouponIssuance,
            request.IdempotencyKey,
            ActionNames.CouponCreate,
            ct => _client.CreateCouponAsync(request.Coupon, request.IdempotencyKey.ToString(), ct).WithKnownEntityId(request.Coupon.CouponCode),
            JsonContextSerializer.Serialize(request.Coupon),
            cancellationToken);
}
