using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KartAdminService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminActionTraceParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "trace_parent",
                table: "admin_actions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "trace_parent",
                table: "admin_actions");
        }
    }
}
