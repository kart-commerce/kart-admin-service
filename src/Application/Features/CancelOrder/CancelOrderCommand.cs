using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.CancelOrder;

/// <summary>api-contract.yaml POST /admin/orders/{orderId}/cancel (Order Management (Admin) flow #7).</summary>
public sealed record CancelOrderCommand(
    string ActingPrincipalId,
    Guid OrderId,
    string? Reason,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
