using Kart.Shared.Domain;
using KartAdminService.Application.Common.Models;

namespace KartAdminService.Application.Common.Interfaces;

/// <summary>Adapter wrapping Category Service's own write API (architecture.md Dependencies table).</summary>
public interface ICategoryServiceClient
{
    Task<Result<string>> CreateCategoryAsync(CategoryWriteRequest request, string idempotencyKey, CancellationToken cancellationToken);

    Task<Result> UpdateCategoryAsync(string categoryId, CategoryWriteRequest request, string ifMatch, string idempotencyKey, CancellationToken cancellationToken);

    Task<Result> ReorderCategoryAsync(string categoryId, int displayOrder, string idempotencyKey, CancellationToken cancellationToken);

    Task<Result> MoveCategoryAsync(string categoryId, string? newParentId, string idempotencyKey, CancellationToken cancellationToken);
}
