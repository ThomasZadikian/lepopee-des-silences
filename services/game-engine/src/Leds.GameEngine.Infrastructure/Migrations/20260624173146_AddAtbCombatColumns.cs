using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAtbCombatColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "atb_fill_per_tick",
                table: "run_combatant_runtime_states",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "current_tick",
                table: "run_active_combats",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "atb_fill_per_tick",
                table: "run_combatant_runtime_states");

            migrationBuilder.DropColumn(
                name: "current_tick",
                table: "run_active_combats");
        }
    }
}
