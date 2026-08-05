using System.Net.Http.Json;
using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;

namespace KartAdminService.Infrastructure.ExternalClients;

/// <summary>
/// Calls Category Service's own write API (architecture.md Dependencies table;
/// kart-category-service/requirement-spec.md §6 item 6: "Category owns its own write model and
/// write API; Admin calls it, never writes Category's tables directly"). Category's admin-facing
/// write route names aren't fixed upstream yet for reorder/move — assumed conventional here.
/// </summary>
public sealed class CategoryServiceClient : ICategoryServiceClient
{
    private readonly HttpClient _httpClient;

    public CategoryServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<Result<string>> CreateCategoryAsync(CategoryWriteRequest request, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Post, "/v1/categories", request, idempotencyKey, ifMatch: null),
            static async (response, ct) =>
            {
                var created = await response.Content.ReadFromJsonAsync<CategoryIdResponse>(cancellationToken: ct);
                return created?.CategoryId ?? string.Empty;
            },
            "Category Service",
            cancellationToken);

    public Task<Result> UpdateCategoryAsync(string categoryId, CategoryWriteRequest request, string ifMatch, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Put, $"/v1/categories/{categoryId}", request, idempotencyKey, ifMatch),
            "Category Service",
            cancellationToken);

    public Task<Result> ReorderCategoryAsync(string categoryId, int displayOrder, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Post, $"/v1/categories/{categoryId}/reorder", new { displayOrder }, idempotencyKey, ifMatch: null),
            "Category Service",
            cancellationToken);

    public Task<Result> MoveCategoryAsync(string categoryId, string? newParentId, string idempotencyKey, CancellationToken cancellationToken) =>
        DownstreamCallResultMapper.ExecuteAsync(
            () => SendAsync(HttpMethod.Post, $"/v1/categories/{categoryId}/move", new { newParentId }, idempotencyKey, ifMatch: null),
            "Category Service",
            cancellationToken);

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, string idempotencyKey, string? ifMatch)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        request.Headers.Add("Idempotency-Key", idempotencyKey);
        if (ifMatch is not null)
        {
            request.Headers.Add("If-Match", ifMatch);
        }

        return _httpClient.SendAsync(request);
    }

    private sealed record CategoryIdResponse(string CategoryId);
}
