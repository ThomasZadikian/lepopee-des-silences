using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistActiveCursesAndAlignLawsWithEffects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "applied_at_utc",
                table: "run_active_palace_laws",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "consumed_at_utc",
                table: "run_active_palace_laws",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "run_active_palace_laws",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "run_active_palace_laws",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "duration",
                table: "run_active_palace_laws",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "expires_at_room_id",
                table: "run_active_palace_laws",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "run_active_curses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false, defaultValue: ""),
                    difficulty_delta = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    curse_definition_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    severity = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    duration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "NextCombatOnly"),
                    expires_at_room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consumed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effect_set_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    RunEntityId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_active_curses", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_active_curses_runs_RunEntityId",
                        column: x => x.RunEntityId,
                        principalTable: "runs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_run_active_curses_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_run_active_palace_laws_key",
                table: "run_active_palace_laws",
                column: "key");

            migrationBuilder.CreateIndex(
                name: "IX_run_active_curses_curse_definition_key",
                table: "run_active_curses",
                column: "curse_definition_key");

            migrationBuilder.CreateIndex(
                name: "IX_run_active_curses_run_id",
                table: "run_active_curses",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_active_curses_RunEntityId",
                table: "run_active_curses",
                column: "RunEntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "run_active_curses");

            migrationBuilder.DropIndex(
                name: "IX_run_active_palace_laws_key",
                table: "run_active_palace_laws");

            migrationBuilder.DropColumn(
                name: "applied_at_utc",
                table: "run_active_palace_laws");

            migrationBuilder.DropColumn(
                name: "consumed_at_utc",
                table: "run_active_palace_laws");

            migrationBuilder.DropColumn(
                name: "description",
                table: "run_active_palace_laws");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "run_active_palace_laws");

            migrationBuilder.DropColumn(
                name: "duration",
                table: "run_active_palace_laws");

            migrationBuilder.DropColumn(
                name: "expires_at_room_id",
                table: "run_active_palace_laws");
        }
    }
}
