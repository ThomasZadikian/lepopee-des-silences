using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerRuntimeStatePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_run_active_combats_runs_run_id",
                table: "run_active_combats");

            migrationBuilder.DropIndex(
                name: "IX_run_active_combats_run_id",
                table: "run_active_combats");

            migrationBuilder.CreateTable(
                name: "run_player_states",
                columns: table => new
                {
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_vitality = table.Column<int>(type: "integer", nullable: false),
                    current_vitality = table.Column<int>(type: "integer", nullable: false),
                    guard = table.Column<int>(type: "integer", nullable: false),
                    mana = table.Column<int>(type: "integer", nullable: false),
                    charge = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_player_states", x => x.run_id);
                    table.ForeignKey(
                        name: "FK_run_player_states_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_player_skills",
                columns: table => new
                {
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    skill_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    targeting_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    effect_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    mana_cost = table.Column<int>(type: "integer", nullable: false),
                    charge_cost = table.Column<int>(type: "integer", nullable: false),
                    base_power = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_player_skills", x => new { x.run_id, x.key });
                    table.ForeignKey(
                        name: "FK_run_player_skills_run_player_states_run_id",
                        column: x => x.run_id,
                        principalTable: "run_player_states",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_runs_active_combat_id",
                table: "runs",
                column: "active_combat_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_run_active_combats_run_id",
                table: "run_active_combats",
                column: "run_id");

            migrationBuilder.AddForeignKey(
                name: "FK_runs_run_active_combats_active_combat_id",
                table: "runs",
                column: "active_combat_id",
                principalTable: "run_active_combats",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_runs_run_active_combats_active_combat_id",
                table: "runs");

            migrationBuilder.DropTable(
                name: "run_player_skills");

            migrationBuilder.DropTable(
                name: "run_player_states");

            migrationBuilder.DropIndex(
                name: "IX_runs_active_combat_id",
                table: "runs");

            migrationBuilder.DropIndex(
                name: "IX_run_active_combats_run_id",
                table: "run_active_combats");

            migrationBuilder.CreateIndex(
                name: "IX_run_active_combats_run_id",
                table: "run_active_combats",
                column: "run_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_run_active_combats_runs_run_id",
                table: "run_active_combats",
                column: "run_id",
                principalTable: "runs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
