using KartAdminService.Domain.Common;
using KartAdminService.Domain.PermissionGrants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KartAdminService.Infrastructure.Persistence.Configurations;

/// <summary>Mirrors database-design.md's admin_permission_grants table literally.</summary>
public sealed class AdminPermissionGrantConfiguration : IEntityTypeConfiguration<AdminPermissionGrant>
{
    public void Configure(EntityTypeBuilder<AdminPermissionGrant> builder)
    {
        builder.ToTable("admin_permission_grants", t => t.HasCheckConstraint(
            "ck_admin_permission_grants_category",
            "category IN ('catalog-management','coupon-issuance','user-suspension','inventory-replenishment','permission-management')"));

        builder.HasKey(g => g.GrantId);
        // Domain.Issue() already generates the id in C# — ValueGeneratedNever so EF always
        // sends it; the DB default is still declared (matching database-design.md literally)
        // for any direct-SQL insert that bypasses this DbContext (e.g. the seed script).
        builder.Property(g => g.GrantId).HasColumnName("grant_id").HasDefaultValueSql("gen_random_uuid()").ValueGeneratedNever();

        builder.Property(g => g.PrincipalId).HasColumnName("principal_id").IsRequired();

        builder.Property(g => g.Category)
            .HasColumnName("category")
            .IsRequired()
            .HasConversion(c => c.ToWireValue(), v => PermissionCategoryExtensions.ParseWireValue(v));

        builder.Property(g => g.GrantedAt).HasColumnName("granted_at").IsRequired();
        builder.Property(g => g.GrantedBy).HasColumnName("granted_by").IsRequired();
        builder.Property(g => g.RevokedAt).HasColumnName("revoked_at");
        builder.Property(g => g.RevokedBy).HasColumnName("revoked_by");

        // Optimistic concurrency for revoke writes (design-decisions.md, "Concurrency Control
        // for Back-Office Writes"). The handler also does its own explicit If-Match precondition
        // check before mutating (a client-supplied stale version must be rejected clearly), so
        // this token is the true-race safety net for two concurrent writers, not the only guard.
        builder.Property(g => g.Version).HasColumnName("version").IsConcurrencyToken();

        // At most one *live* (non-revoked) grant per (principal, category) — the single source
        // of truth "which categories can this Admin-role holder actually exercise"
        // (Domain Invariant #1).
        builder.HasIndex(g => new { g.PrincipalId, g.Category })
            .HasDatabaseName("uq_admin_permission_grants_live")
            .IsUnique()
            .HasFilter("revoked_at IS NULL");
    }
}
