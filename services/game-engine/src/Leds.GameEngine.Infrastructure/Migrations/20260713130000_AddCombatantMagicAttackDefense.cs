using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatantMagicAttackDefense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "magic_attack",
                table: "run_combatant_base_stat_snapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "magic_defense",
                table: "run_combatant_base_stat_snapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "magic_defense",
                table: "run_combatant_base_stat_snapshots");

            migrationBuilder.DropColumn(
                name: "magic_attack",
                table: "run_combatant_base_stat_snapshots");
        }
    }
}
