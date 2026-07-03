using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatantThreatColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "threat_value",
                table: "run_combatant_runtime_states",
                type: "double precision",
                nullable: false,
                defaultValue: 0d);

            migrationBuilder.AddColumn<Guid>(
                name: "last_attacker_id",
                table: "run_combatant_runtime_states",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "threat_value",
                table: "run_combatant_runtime_states");

            migrationBuilder.DropColumn(
                name: "last_attacker_id",
                table: "run_combatant_runtime_states");
        }
    }
}
