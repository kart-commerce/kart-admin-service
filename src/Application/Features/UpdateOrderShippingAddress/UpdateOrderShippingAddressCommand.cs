using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.UpdateOrderShippingAddress;

/// <summary>api-contract.yaml PATCH /admin/orders/{orderId}/shipping-address (Order Management (Admin) flow #7).</summary>
public sealed record UpdateOrderShippingAddressCommand(
    string ActingPrincipalId,
    Guid OrderId,
    ShippingAddressWriteRequest Address,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
