using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.AdminUpdateOrderStatus;

/// <summary>api-contract.yaml PATCH /admin/orders/{orderId}/status — ops-recovery manual status advance, proxied to kart-order-service which itself restricts TargetStatus to {Shipped, Delivered, FulfillmentException}.</summary>
public sealed record AdminUpdateOrderStatusCommand(
    string ActingPrincipalId,
    Guid OrderId,
    string TargetStatus,
    string Reason,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
