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
    public Result<string> GetProductGroupIdResult { get; set; } = Result.Success("product-group-1");
    public Result UpdateResult { get; set; } = Result.Success();
    public Result UpdatePriceResult { get; set; } = Result.Success();
    public Result DeactivateResult { get; set; } = Result.Success();

    public Task<Result<string>> CreateProductAsync(ProductWriteRequest request, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(CreateResult);
    public Task<Result<string>> GetProductGroupIdAsync(string sku, CancellationToken cancellationToken) => Task.FromResult(GetProductGroupIdResult);
    public Task<Result> UpdateProductAsync(string productGroupId, ProductWriteRequest request, string ifMatch, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(UpdateResult);
    public Task<Result> UpdatePriceAsync(string sku, MoneyDto price, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(UpdatePriceResult);
    public Task<Result> DeactivateProductAsync(string productGroupId, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(DeactivateResult);
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

public sealed class FakeAttributeServiceClient : IAttributeServiceClient
{
    public Result<string> CreateResult { get; set; } = Result.Success("attribute-1");
    public Result UpdateResult { get; set; } = Result.Success();
    public Result DeprecateResult { get; set; } = Result.Success();

    public Task<Result<string>> CreateAttributeAsync(AttributeWriteRequest request, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(CreateResult);
    public Task<Result> UpdateAttributeAsync(string attributeId, AttributeUpdateRequest request, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(UpdateResult);
    public Task<Result> DeprecateAttributeAsync(string attributeId, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(DeprecateResult);
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

public sealed class FakeOrderServiceClient : IOrderServiceClient
{
    public Result CancelResult { get; set; } = Result.Success();
    public Result UpdateStatusResult { get; set; } = Result.Success();
    public Result UpdateShippingAddressResult { get; set; } = Result.Success();
    public Result RequestShipmentResult { get; set; } = Result.Success();
    public Result ResolveFulfillmentExceptionResult { get; set; } = Result.Success();

    public Task<Result> CancelOrderAsync(Guid orderId, string? reason, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(CancelResult);
    public Task<Result> UpdateStatusAsync(Guid orderId, string targetStatus, string reason, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(UpdateStatusResult);
    public Task<Result> UpdateShippingAddressAsync(Guid orderId, ShippingAddressWriteRequest address, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(UpdateShippingAddressResult);
    public Task<Result> RequestShipmentAsync(Guid orderId, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(RequestShipmentResult);
    public Task<Result> ResolveFulfillmentExceptionAsync(Guid orderId, string action, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(ResolveFulfillmentExceptionResult);
}
