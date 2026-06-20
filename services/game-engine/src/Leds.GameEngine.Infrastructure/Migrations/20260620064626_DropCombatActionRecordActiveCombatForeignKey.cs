using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropCombatActionRecordActiveCombatForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_run_combat_actions_run_active_combats_combat_id",
                table: "run_combat_actions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_run_combat_actions_run_active_combats_combat_id",
                table: "run_combat_actions",
                column: "combat_id",
                principalTable: "run_active_combats",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
