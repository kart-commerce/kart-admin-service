using KartAdminService.Domain.Actions;
using KartAdminService.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KartAdminService.Infrastructure.Persistence.Configurations;

/// <summary>Mirrors database-design.md's admin_actions table literally — Admin's audit trail AND its Outbox row for AdminActionPerformed, in one table.</summary>
public sealed class AdminActionConfiguration : IEntityTypeConfiguration<AdminAction>
{
    public void Configure(EntityTypeBuilder<AdminAction> builder)
    {
        builder.ToTable("admin_actions", t => t.HasCheckConstraint(
            "ck_admin_actions_category",
            "category IN ('catalog-management','coupon-issuance','user-suspension','inventory-replenishment','permission-management','order-management')"));

        builder.HasKey(a => a.ActionId);
        builder.Property(a => a.ActionId).HasColumnName("action_id").HasDefaultValueSql("gen_random_uuid()").ValueGeneratedNever();

        builder.Property(a => a.IdempotencyKey).HasColumnName("idempotency_key").IsRequired();
        builder.Property(a => a.AdminId).HasColumnName("admin_id").IsRequired();

        builder.Property(a => a.Category)
            .HasColumnName("category")
            .IsRequired()
            .HasConversion(c => c.ToWireValue(), v => PermissionCategoryExtensions.ParseWireValue(v));

        builder.Property(a => a.Action).HasColumnName("action").IsRequired();
        builder.Property(a => a.EntityId).HasColumnName("entity_id").IsRequired();
        builder.Property(a => a.Context).HasColumnName("context").HasColumnType("jsonb");
        builder.Property(a => a.PerformedAt).HasColumnName("performed_at").IsRequired();
        builder.Property(a => a.PublishedAt).HasColumnName("published_at");
        builder.Property(a => a.PublishedBy).HasColumnName("published_by");
        builder.Property(a => a.TraceParent).HasColumnName("trace_parent");

        // Dedupe check before/while retrying an admin-action attempt (design-decisions.md,
        // "Idempotency Mechanism for Outbound Write Calls") — a retried attempt must never
        // produce two local audit rows for one logical action. This is the actual race-safety
        // net AdminActionRepository.AddAndCommitOrGetExistingAsync depends on.
        builder.HasIndex(a => a.IdempotencyKey).HasDatabaseName("uq_admin_actions_idempotency_key").IsUnique();

        // The Outbox poller's "find rows not yet published" scan — a cheap partial-index range
        // scan rather than a full-table scan as this 5-year-retention audit table grows.
        builder.HasIndex(a => a.PerformedAt).HasDatabaseName("idx_admin_actions_unpublished").HasFilter("published_at IS NULL");

        // "What did this admin do, in which category, over what window" — the compliance/audit
        // review query pattern GET /admin/actions (ADM-16) and Analytics' dashboard both run.
        builder.HasIndex(a => new { a.AdminId, a.Category, a.PerformedAt }).HasDatabaseName("idx_admin_actions_admin_category");
    }
}
