using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveCombatPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "run_active_combats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    turn_number = table.Column<int>(type: "integer", nullable: false),
                    active_combatant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_active_combats", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_active_combats_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_combatants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    side = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    archetype = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    max_vitality = table.Column<int>(type: "integer", nullable: false),
                    current_vitality = table.Column<int>(type: "integer", nullable: false),
                    guard = table.Column<int>(type: "integer", nullable: false),
                    mana = table.Column<int>(type: "integer", nullable: false),
                    charge = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_combatants", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_combatants_run_active_combats_combat_id",
                        column: x => x.combat_id,
                        principalTable: "run_active_combats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_combatant_skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combatant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    skill_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    targeting_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    effect_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    mana_cost = table.Column<int>(type: "integer", nullable: false),
                    charge_cost = table.Column<int>(type: "integer", nullable: false),
                    base_power = table.Column<int>(type: "integer", nullable: false),
                    tags = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_combatant_skills", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_combatant_skills_run_combatants_combatant_id",
                        column: x => x.combatant_id,
                        principalTable: "run_combatants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_run_active_combats_run_id",
                table: "run_active_combats",
                column: "run_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_run_active_combats_status",
                table: "run_active_combats",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_run_combatant_skills_combatant_id",
                table: "run_combatant_skills",
                column: "combatant_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_combatant_skills_key",
                table: "run_combatant_skills",
                column: "key");

            migrationBuilder.CreateIndex(
                name: "IX_run_combatants_combat_id",
                table: "run_combatants",
                column: "combat_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_combatants_side",
                table: "run_combatants",
                column: "side");

            migrationBuilder.CreateIndex(
                name: "IX_run_combatants_status",
                table: "run_combatants",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "run_combatant_skills");

            migrationBuilder.DropTable(
                name: "run_combatants");

            migrationBuilder.DropTable(
                name: "run_active_combats");
        }
    }
}
