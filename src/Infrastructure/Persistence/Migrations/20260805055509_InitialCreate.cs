using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KartAdminService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_actions",
                columns: table => new
                {
                    action_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_id = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: false),
                    context = table.Column<string>(type: "jsonb", nullable: true),
                    performed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_actions", x => x.action_id);
                    table.CheckConstraint("ck_admin_actions_category", "category IN ('catalog-management','coupon-issuance','user-suspension','inventory-replenishment','permission-management')");
                });

            migrationBuilder.CreateTable(
                name: "admin_permission_grants",
                columns: table => new
                {
                    grant_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    principal_id = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by = table.Column<string>(type: "text", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_permission_grants", x => x.grant_id);
                    table.CheckConstraint("ck_admin_permission_grants_category", "category IN ('catalog-management','coupon-issuance','user-suspension','inventory-replenishment','permission-management')");
                });

            migrationBuilder.CreateIndex(
                name: "idx_admin_actions_admin_category",
                table: "admin_actions",
                columns: new[] { "admin_id", "category", "performed_at" });

            migrationBuilder.CreateIndex(
                name: "idx_admin_actions_unpublished",
                table: "admin_actions",
                column: "performed_at",
                filter: "published_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_admin_actions_idempotency_key",
                table: "admin_actions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_admin_permission_grants_live",
                table: "admin_permission_grants",
                columns: new[] { "principal_id", "category" },
                unique: true,
                filter: "revoked_at IS NULL");

            // database-design.md's Row-Level Security Policy (BRD S24.1.4): a principal
            // legitimately needs to read/write *another* principal's row here — issuing or
            // revoking someone else's grant is exactly what the permission-management
            // meta-category exists to do. The policy mirrors the same live-grant check the
            // application layer already runs (uq_admin_permission_grants_live), expressed as a
            // same-table subquery. AdminDbContext.SaveChangesAsync/BeginPrincipalScopeAsync
            // issues `SET LOCAL app.current_principal` inside the same explicit transaction
            // this policy reads from.
            //
            // FORCE ROW LEVEL SECURITY is required for this policy to apply to the table's own
            // owner - Postgres never restricts the owner (and never restricts a superuser,
            // regardless of FORCE) by default. Every Kart service currently connects to a single
            // shared `postgres` superuser role in local dev (kart-devops' postgres-init.sql:
            // "not a security boundary... kart-infra's kind/Helm path is where per-service DB
            // user isolation actually matters") - this policy is therefore structurally correct
            // and ready today, but only takes effect once a deployment provisions Admin's own
            // non-superuser application role, per that same already-documented platform
            // convention. Not something this migration can fix on its own.
            migrationBuilder.Sql("ALTER TABLE admin_permission_grants ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE admin_permission_grants FORCE ROW LEVEL SECURITY;");

            // A policy's own subquery against the *same* RLS-protected table re-triggers that
            // same policy's evaluation on every row the subquery touches, which itself runs the
            // subquery again, and so on - Postgres detects this as "infinite recursion detected
            // in policy" rather than looping forever. The standard, documented fix is to move
            // the self-referencing lookup into a SECURITY DEFINER function with `row_security =
            // off`, so it reads the table with the function owner's own privileges and never
            // re-enters the calling policy.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION admin_has_live_permission_management_grant(p_principal_id text)
                RETURNS boolean
                LANGUAGE sql
                SECURITY DEFINER
                STABLE
                SET row_security = off
                AS $$
                    SELECT EXISTS (
                        SELECT 1 FROM admin_permission_grants
                        WHERE principal_id = p_principal_id
                          AND category = 'permission-management'
                          AND revoked_at IS NULL
                    );
                $$;
                """);
            migrationBuilder.Sql("GRANT EXECUTE ON FUNCTION admin_has_live_permission_management_grant(text) TO PUBLIC;");

            migrationBuilder.Sql(
                """
                CREATE POLICY admin_permission_grants_self_or_grant_manager ON admin_permission_grants
                    USING (
                        principal_id = current_setting('app.current_principal', true)
                        OR admin_has_live_permission_management_grant(current_setting('app.current_principal', true))
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS admin_permission_grants_self_or_grant_manager ON admin_permission_grants;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS admin_has_live_permission_management_grant(text);");

            migrationBuilder.DropTable(
                name: "admin_actions");

            migrationBuilder.DropTable(
                name: "admin_permission_grants");
        }
    }
}
