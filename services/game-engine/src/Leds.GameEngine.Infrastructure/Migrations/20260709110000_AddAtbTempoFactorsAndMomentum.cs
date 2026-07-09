using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAtbTempoFactorsAndMomentum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "atb_tempo_room_factor_per_mille",
                table: "run_combatant_runtime_states",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "atb_tempo_combatant_factor_per_mille",
                table: "run_combatant_runtime_states",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tempo_momentum_per_mille",
                table: "run_combatant_runtime_states",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "atb_tempo_room_factor_per_mille",
                table: "run_combatant_runtime_states");

            migrationBuilder.DropColumn(
                name: "atb_tempo_combatant_factor_per_mille",
                table: "run_combatant_runtime_states");

            migrationBuilder.DropColumn(
                name: "tempo_momentum_per_mille",
                table: "run_combatant_runtime_states");
        }
    }
}
