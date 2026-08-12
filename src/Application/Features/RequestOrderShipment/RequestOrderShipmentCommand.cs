using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;
using MediatR;

namespace KartAdminService.Application.Features.RequestOrderShipment;

/// <summary>api-contract.yaml POST /admin/orders/{orderId}/request-shipment (Order Management (Admin) flow #7's "Trigger Shipment").</summary>
public sealed record RequestOrderShipmentCommand(
    string ActingPrincipalId,
    Guid OrderId,
    Guid IdempotencyKey) : IRequest<Result<AdminActionResultDto>>;
