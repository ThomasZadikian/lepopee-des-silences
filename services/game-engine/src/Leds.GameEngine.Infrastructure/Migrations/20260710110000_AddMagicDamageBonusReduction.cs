using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMagicDamageBonusReduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "magic_damage_bonus_percent",
                table: "runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "magic_damage_reduction_percent",
                table: "runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "magic_damage_bonus_percent",
                table: "run_combatants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "magic_damage_reduction_percent",
                table: "run_combatants",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "magic_damage_bonus_percent",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "magic_damage_reduction_percent",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "magic_damage_bonus_percent",
                table: "run_combatants");

            migrationBuilder.DropColumn(
                name: "magic_damage_reduction_percent",
                table: "run_combatants");
        }
    }
}
