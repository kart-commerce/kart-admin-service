using System.Net.Http.Json;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;

namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>Calls Order Service's own admin-gated write API (kart-order-service/src/Api/Controllers/OrdersController.cs), same SendAsync-helper shape as CategoryServiceClient.</summary>
public sealed class OrderServiceClient : IOrderServiceClient
{
    private readonly HttpClient _httpClient;

    public OrderServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<Result> CancelOrderAsync(Guid orderId, string? reason, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Post, $"/v1/orders/{orderId}/cancel", reason is null ? null : new { reason }, idempotencyKey),
            "Order Service",
            cancellationToken);

    public Task<Result> UpdateStatusAsync(Guid orderId, string targetStatus, string reason, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Patch, $"/v1/orders/{orderId}/status", new { targetStatus, reason }, idempotencyKey),
            "Order Service",
            cancellationToken);

    public Task<Result> UpdateShippingAddressAsync(Guid orderId, ShippingAddressWriteRequest address, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Patch, $"/v1/orders/{orderId}/shipping-address", address, idempotencyKey),
            "Order Service",
            cancellationToken);

    public Task<Result> RequestShipmentAsync(Guid orderId, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Post, $"/v1/orders/{orderId}/request-shipment", body: null, idempotencyKey),
            "Order Service",
            cancellationToken);

    public Task<Result> ResolveFulfillmentExceptionAsync(Guid orderId, string action, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Post, $"/v1/orders/{orderId}/resolve-fulfillment-exception", new { action }, idempotencyKey),
            "Order Service",
            cancellationToken);

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, string idempotencyKey)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return _httpClient.SendAsync(request);
    }
}
