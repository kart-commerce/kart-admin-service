using System.Net.Http.Json;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;

namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>Calls Offer Service's admin-only Coupon write API (architecture.md Dependencies table; ADR-0001 — Offer owns the Coupon aggregate).</summary>
public sealed class OfferServiceClient : IOfferServiceClient
{
    private readonly HttpClient _httpClient;

    public OfferServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<Result> CreateCouponAsync(CouponWriteRequest request, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Post, "/v1/coupons", request, idempotencyKey),
            "Offer Service",
            cancellationToken);

    public Task<Result> DeactivateCouponAsync(string couponCode, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Post, $"/v1/coupons/{couponCode}/deactivate", body: null, idempotencyKey),
            "Offer Service",
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
