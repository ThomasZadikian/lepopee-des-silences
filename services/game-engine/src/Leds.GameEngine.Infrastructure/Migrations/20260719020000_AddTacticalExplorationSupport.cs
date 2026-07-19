using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// HAND-AUTHORED REFERENCE MIGRATION — this environment has no dotnet SDK, so this
    /// file (and any paired Designer.cs/ModelSnapshot.cs) could not be generated or
    /// verified by the EF tooling. Every column added here is purely additive and
    /// nullable (or defaulted for the non-nullable "ExplorationMode" flag, matching the
    /// existing "LawDenialEnabled"/"CaliceInfiniEnabled" precedent) — existing Classic-mode
    /// rows are entirely unaffected: their Grid* columns simply read as NULL, and
    /// ExplorationMode reads as "Classic".
    ///
    /// Before merging, run locally:
    ///   dotnet ef migrations add AddTacticalExplorationSupport
    /// and use this file's Up()/Down() only as a cross-check against what the tool
    /// generates (it will also regenerate a correct Designer.cs + ModelSnapshot.cs,
    /// which must NOT be hand-written).
    /// </remarks>
    public partial class AddTacticalExplorationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExplorationMode",
                table: "runs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Classic");

            migrationBuilder.AddColumn<int>(
                name: "grid_width",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "grid_height",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "grid_movement_budget",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "grid_movement_budget_remaining",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "grid_start_x",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "grid_start_y",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "grid_party_x",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "grid_party_y",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "grid_revealed_node_ids_csv",
                table: "run_rooms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "grid_revealed_cells_csv",
                table: "run_rooms",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "grid_revealed_cells_csv",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_revealed_node_ids_csv",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_party_y",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_party_x",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_start_y",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_start_x",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_movement_budget_remaining",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_movement_budget",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_height",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_width",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "ExplorationMode",
                table: "runs");
        }
    }
}
