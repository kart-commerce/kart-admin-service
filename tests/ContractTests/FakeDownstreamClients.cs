using Kart.Shared.Domain;
using KartAdminService.Application.Common.Interfaces;
using KartAdminService.Application.Common.Models;

namespace KartAdminService.ContractTests;

/// <summary>
/// Fake downstream owning-service clients - contract tests assert Admin's own HTTP wire contract,
/// never a live Product/Category/Offer/Identity/Inventory dependency. Each defaults to success;
/// tests configure a specific instance's Result to exercise a failure-mapping path (404/409/503).
/// </summary>
public sealed class FakeProductServiceClient : IProductServiceClient
{
    public Result<string> CreateResult { get; set; } = Result.Success("product-1");
    public Result UpdateResult { get; set; } = Result.Success();
    public Result DeactivateResult { get; set; } = Result.Success();

    public Task<Result<string>> CreateProductAsync(ProductWriteRequest request, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(CreateResult);
    public Task<Result> UpdateProductAsync(string productId, ProductWriteRequest request, string ifMatch, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(UpdateResult);
    public Task<Result> DeactivateProductAsync(string productId, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(DeactivateResult);
}

public sealed class FakeCategoryServiceClient : ICategoryServiceClient
{
    public Result<string> CreateResult { get; set; } = Result.Success("category-1");
    public Result UpdateResult { get; set; } = Result.Success();
    public Result ReorderResult { get; set; } = Result.Success();
    public Result MoveResult { get; set; } = Result.Success();

    public Task<Result<string>> CreateCategoryAsync(CategoryWriteRequest request, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(CreateResult);
    public Task<Result> UpdateCategoryAsync(string categoryId, CategoryWriteRequest request, string ifMatch, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(UpdateResult);
    public Task<Result> ReorderCategoryAsync(string categoryId, int displayOrder, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(ReorderResult);
    public Task<Result> MoveCategoryAsync(string categoryId, string? newParentId, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(MoveResult);
}

public sealed class FakeOfferServiceClient : IOfferServiceClient
{
    public Result CreateResult { get; set; } = Result.Success();
    public Result DeactivateResult { get; set; } = Result.Success();

    public Task<Result> CreateCouponAsync(CouponWriteRequest request, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(CreateResult);
    public Task<Result> DeactivateCouponAsync(string couponCode, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(DeactivateResult);
}

public sealed class FakeIdentityAdminClient : IIdentityAdminClient
{
    public Result LockResult { get; set; } = Result.Success();
    public Result UnlockResult { get; set; } = Result.Success();

    public Task<Result> LockUserAsync(string userId, string? reason, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(LockResult);
    public Task<Result> UnlockUserAsync(string userId, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(UnlockResult);
}

public sealed class FakeInventoryServiceClient : IInventoryServiceClient
{
    public Result ReplenishResult { get; set; } = Result.Success();

    public Task<Result> ReplenishAsync(string sku, string warehouseId, int qtyAdded, string? reason, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(ReplenishResult);
}
