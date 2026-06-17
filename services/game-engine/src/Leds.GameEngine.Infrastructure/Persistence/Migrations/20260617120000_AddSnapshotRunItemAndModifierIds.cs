using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotRunItemAndModifierIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "snapshot_run_item_ids",
                table: "runs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "snapshot_run_modifier_ids",
                table: "runs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "snapshot_run_item_ids",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "snapshot_run_modifier_ids",
                table: "runs");
        }
    }
}
