using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatDuelAndDotDurationExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "dot_duration_extension_ticks",
                table: "run_active_combats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "duel_damage_asymmetry_enabled",
                table: "run_active_combats",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dot_duration_extension_ticks",
                table: "run_active_combats");

            migrationBuilder.DropColumn(
                name: "duel_damage_asymmetry_enabled",
                table: "run_active_combats");
        }
    }
}
