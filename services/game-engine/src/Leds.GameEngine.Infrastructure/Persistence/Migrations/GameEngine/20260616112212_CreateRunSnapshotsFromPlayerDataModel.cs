using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations.GameEngine
{
    /// <inheritdoc />
    public partial class CreateRunSnapshotsFromPlayerDataModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "run_player_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_player_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_player_snapshots_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_character_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_character_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_character_snapshots_run_player_snapshots_player_snapsho~",
                        column: x => x.player_snapshot_id,
                        principalTable: "run_player_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_character_skill_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    character_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    skill_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    targeting_mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    effect_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    mana_cost = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    charge_cost = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    base_power = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_character_skill_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_character_skill_snapshots_run_character_snapshots_chara~",
                        column: x => x.character_snapshot_id,
                        principalTable: "run_character_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_character_stat_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    character_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_vitality = table.Column<int>(type: "integer", nullable: false),
                    attack_power = table.Column<int>(type: "integer", nullable: false),
                    defense = table.Column<int>(type: "integer", nullable: false),
                    starting_guard = table.Column<int>(type: "integer", nullable: false),
                    speed = table.Column<int>(type: "integer", nullable: false),
                    initiative = table.Column<int>(type: "integer", nullable: false),
                    recovery = table.Column<int>(type: "integer", nullable: false),
                    focus = table.Column<int>(type: "integer", nullable: false),
                    mana = table.Column<int>(type: "integer", nullable: false),
                    charge = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_character_stat_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_character_stat_snapshots_run_character_snapshots_charac~",
                        column: x => x.character_snapshot_id,
                        principalTable: "run_character_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_run_character_skill_snapshots_character_snapshot_id",
                table: "run_character_skill_snapshots",
                column: "character_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_character_skill_snapshots_skill_definition_key",
                table: "run_character_skill_snapshots",
                column: "skill_definition_key");

            migrationBuilder.CreateIndex(
                name: "IX_run_character_snapshots_player_snapshot_id",
                table: "run_character_snapshots",
                column: "player_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_character_stat_snapshots_character_snapshot_id",
                table: "run_character_stat_snapshots",
                column: "character_snapshot_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_run_player_snapshots_run_id",
                table: "run_player_snapshots",
                column: "run_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "run_character_skill_snapshots");

            migrationBuilder.DropTable(
                name: "run_character_stat_snapshots");

            migrationBuilder.DropTable(
                name: "run_character_snapshots");

            migrationBuilder.DropTable(
                name: "run_player_snapshots");
        }
    }
}
