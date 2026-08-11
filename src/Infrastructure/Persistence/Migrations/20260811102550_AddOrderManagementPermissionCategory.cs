using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KartAdminService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderManagementPermissionCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_admin_permission_grants_category",
                table: "admin_permission_grants");

            migrationBuilder.DropCheckConstraint(
                name: "ck_admin_actions_category",
                table: "admin_actions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_admin_permission_grants_category",
                table: "admin_permission_grants",
                sql: "category IN ('catalog-management','coupon-issuance','user-suspension','inventory-replenishment','permission-management','order-management')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_admin_actions_category",
                table: "admin_actions",
                sql: "category IN ('catalog-management','coupon-issuance','user-suspension','inventory-replenishment','permission-management','order-management')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_admin_permission_grants_category",
                table: "admin_permission_grants");

            migrationBuilder.DropCheckConstraint(
                name: "ck_admin_actions_category",
                table: "admin_actions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_admin_permission_grants_category",
                table: "admin_permission_grants",
                sql: "category IN ('catalog-management','coupon-issuance','user-suspension','inventory-replenishment','permission-management')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_admin_actions_category",
                table: "admin_actions",
                sql: "category IN ('catalog-management','coupon-issuance','user-suspension','inventory-replenishment','permission-management')");
        }
    }
}
