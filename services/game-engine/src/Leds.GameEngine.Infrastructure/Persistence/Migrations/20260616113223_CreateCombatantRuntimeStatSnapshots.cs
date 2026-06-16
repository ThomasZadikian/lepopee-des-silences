using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateCombatantRuntimeStatSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "run_combatant_base_stat_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combatant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_vitality = table.Column<int>(type: "integer", nullable: false),
                    attack_power = table.Column<int>(type: "integer", nullable: false),
                    defense = table.Column<int>(type: "integer", nullable: false),
                    starting_guard = table.Column<int>(type: "integer", nullable: false),
                    speed = table.Column<int>(type: "integer", nullable: false),
                    initiative = table.Column<int>(type: "integer", nullable: false),
                    recovery = table.Column<int>(type: "integer", nullable: false),
                    focus = table.Column<int>(type: "integer", nullable: false),
                    mana = table.Column<int>(type: "integer", nullable: false),
                    charge = table.Column<int>(type: "integer", nullable: false),
                    atb_ready_threshold = table.Column<int>(type: "integer", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_combatant_base_stat_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_combatant_base_stat_snapshots_run_combatants_combatant_~",
                        column: x => x.combatant_id,
                        principalTable: "run_combatants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_combatant_runtime_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combatant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_vitality = table.Column<int>(type: "integer", nullable: false),
                    current_guard = table.Column<int>(type: "integer", nullable: false),
                    current_focus = table.Column<int>(type: "integer", nullable: false),
                    current_mana = table.Column<int>(type: "integer", nullable: false),
                    current_charge = table.Column<int>(type: "integer", nullable: false),
                    atb_gauge_value = table.Column<int>(type: "integer", nullable: true),
                    action_recovery_until_tick = table.Column<int>(type: "integer", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_combatant_runtime_states", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_combatant_runtime_states_run_combatants_combatant_id",
                        column: x => x.combatant_id,
                        principalTable: "run_combatants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_run_combatant_base_stat_snapshots_combatant_id",
                table: "run_combatant_base_stat_snapshots",
                column: "combatant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_run_combatant_runtime_states_combatant_id",
                table: "run_combatant_runtime_states",
                column: "combatant_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "run_combatant_base_stat_snapshots");

            migrationBuilder.DropTable(
                name: "run_combatant_runtime_states");
        }
    }
}
