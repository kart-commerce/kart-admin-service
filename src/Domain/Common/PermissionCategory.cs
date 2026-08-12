namespace KartAdminService.Domain.Common;

/// <summary>
/// The five fixed categories requirement-spec.md §6 Decision item 1 and database-design.md's
/// CHECK constraint define — four business categories plus the `permission-management`
/// meta-category that governs granting/revoking the other four. Shared value object used by
/// both aggregates (ddd-model.md Modeling Decision #2) so there is exactly one definition of
/// "what a category is," not two independently-drifting ones. This is a closed value set —
/// extending it requires a new requirement-spec decision, not a code-level enum add
/// (requirement-spec.md §6 Decision item 1's own accepted trade-off).
/// </summary>
public enum PermissionCategory
{
    CatalogManagement,
    CouponIssuance,
    UserSuspension,
    InventoryReplenishment,
    PermissionManagement,

    /// <summary>
    /// Added for the "Order Management (Admin)" flow — gates Cancel/Update Status/Update Shipping
    /// Address/Request Shipment/Resolve Fulfillment Exception. Per this enum's own closed-set
    /// doc comment, extending it is a deliberate requirement decision, not a casual add — tracked
    /// via the accompanying EF migration that widens both `admin_actions`/`admin_permission_grants`
    /// CHECK constraints to accept the new `order-management` wire value.
    /// </summary>
    OrderManagement,
}

public static class PermissionCategoryExtensions
{
    /// <summary>Db/wire representation — kebab-case, matching database-design.md's CHECK constraint literals and api-contract.yaml's GrantCategory enum.</summary>
    public static string ToWireValue(this PermissionCategory category) => category switch
    {
        PermissionCategory.CatalogManagement => "catalog-management",
        PermissionCategory.CouponIssuance => "coupon-issuance",
        PermissionCategory.UserSuspension => "user-suspension",
        PermissionCategory.InventoryReplenishment => "inventory-replenishment",
        PermissionCategory.PermissionManagement => "permission-management",
        PermissionCategory.OrderManagement => "order-management",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown permission category."),
    };

    public static PermissionCategory ParseWireValue(string value) => value switch
    {
        "catalog-management" => PermissionCategory.CatalogManagement,
        "coupon-issuance" => PermissionCategory.CouponIssuance,
        "user-suspension" => PermissionCategory.UserSuspension,
        "inventory-replenishment" => PermissionCategory.InventoryReplenishment,
        "permission-management" => PermissionCategory.PermissionManagement,
        "order-management" => PermissionCategory.OrderManagement,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown permission category."),
    };
}
