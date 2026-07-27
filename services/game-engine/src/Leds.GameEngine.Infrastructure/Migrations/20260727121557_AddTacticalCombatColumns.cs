using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTacticalCombatColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "kind",
                table: "run_active_combats",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Atb");

            migrationBuilder.AddColumn<int>(
                name: "tactical_active_index",
                table: "run_active_combats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tactical_elevation_csv",
                table: "run_active_combats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tactical_height",
                table: "run_active_combats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tactical_initiative_order_csv",
                table: "run_active_combats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tactical_positions_csv",
                table: "run_active_combats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tactical_round_number",
                table: "run_active_combats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tactical_turn_states_csv",
                table: "run_active_combats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tactical_walkable_csv",
                table: "run_active_combats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tactical_width",
                table: "run_active_combats",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "kind",
                table: "run_active_combats");

            migrationBuilder.DropColumn(
                name: "tactical_active_index",
                table: "run_active_combats");

            migrationBuilder.DropColumn(
                name: "tactical_elevation_csv",
                table: "run_active_combats");

            migrationBuilder.DropColumn(
                name: "tactical_height",
                table: "run_active_combats");

            migrationBuilder.DropColumn(
                name: "tactical_initiative_order_csv",
                table: "run_active_combats");

            migrationBuilder.DropColumn(
                name: "tactical_positions_csv",
                table: "run_active_combats");

            migrationBuilder.DropColumn(
                name: "tactical_round_number",
                table: "run_active_combats");

            migrationBuilder.DropColumn(
                name: "tactical_turn_states_csv",
                table: "run_active_combats");

            migrationBuilder.DropColumn(
                name: "tactical_walkable_csv",
                table: "run_active_combats");

            migrationBuilder.DropColumn(
                name: "tactical_width",
                table: "run_active_combats");
        }
    }
}
