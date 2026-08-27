namespace KartAdminService.Domain.Common;

/// <summary>
/// The specific operation names within a category (database-design.md's admin_actions.action
/// column comment) — the AdminActionPerformed event's own `action` payload field. Named
/// constants per coding-standards.md ("no magic numbers/strings — name the constant, even if
/// only used once, when the value's meaning isn't obvious from context") rather than inline
/// string literals scattered across 16 feature handlers.
/// </summary>
public static class ActionNames
{
    public const string GrantIssue = "grant.issue";
    public const string GrantRevoke = "grant.revoke";
    public const string ProductCreate = "product.create";
    public const string ProductUpdate = "product.update";
    public const string ProductDeactivate = "product.deactivate";
    public const string CategoryCreate = "category.create";
    public const string CategoryUpdate = "category.update";
    public const string CategoryReorder = "category.reorder";
    public const string CategoryMove = "category.move";
    public const string AttributeCreate = "attribute.create";
    public const string AttributeUpdate = "attribute.update";
    public const string AttributeDeprecate = "attribute.deprecate";
    public const string CouponCreate = "coupon.create";
    public const string CouponDeactivate = "coupon.deactivate";
    public const string UserLock = "user.lock";
    public const string UserUnlock = "user.unlock";
    public const string InventoryReplenish = "inventory.replenish";

    // Inventory & Stock Management flow — the remaining admin write paths beyond replenish.
    public const string InventoryProvision = "inventory.provision";
    public const string InventoryUpdateThreshold = "inventory.threshold.update";
    public const string InventoryReconcile = "inventory.reconcile";

    // Order Management (Admin) flow #7 — proxies to kart-order-service's own admin-gated endpoints.
    public const string OrderCancel = "order.cancel";
    public const string OrderStatusUpdate = "order.status.admin_update";
    public const string OrderShippingAddressUpdate = "order.shipping_address.update";
    public const string OrderShipmentRequest = "order.shipment.request";
    public const string OrderFulfillmentExceptionResolve = "order.fulfillment_exception.resolve";
}
