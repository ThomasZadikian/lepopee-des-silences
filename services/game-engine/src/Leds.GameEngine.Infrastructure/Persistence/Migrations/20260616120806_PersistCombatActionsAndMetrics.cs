using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistCombatActionsAndMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "run_combat_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    turn_number = table.Column<int>(type: "integer", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_side = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    skill_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    skill_display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    target_ids = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    source_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    source_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    raw_damage = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    mitigated_damage = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    vitality_damage = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    guard_damage = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    guard_absorbed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    guard_gained = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    damage_dealt = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    damage_taken = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    healing_done = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    healing_received = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    effects_applied = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_combat_actions", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_combat_actions_run_active_combats_combat_id",
                        column: x => x.combat_id,
                        principalTable: "run_active_combats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_run_combat_actions_actor_id",
                table: "run_combat_actions",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_combat_actions_combat_id",
                table: "run_combat_actions",
                column: "combat_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_combat_actions_occurred_at_utc",
                table: "run_combat_actions",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_run_combat_actions_turn_number",
                table: "run_combat_actions",
                column: "turn_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "run_combat_actions");
        }
    }
}
