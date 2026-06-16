using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPalaceAdaptiveInfluenceProjections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "run_adaptive_influences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    influence_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    influence_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    value_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    duration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_adaptive_influences", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_adaptive_influences_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_palace_indicator_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    indicator_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    narrative_text = table.Column<string>(type: "text", nullable: false),
                    intensity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_decision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_palace_indicator_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_palace_indicator_snapshots_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_run_adaptive_influences_influence_tag",
                table: "run_adaptive_influences",
                column: "influence_tag");

            migrationBuilder.CreateIndex(
                name: "IX_run_adaptive_influences_run_id",
                table: "run_adaptive_influences",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_adaptive_influences_source_type",
                table: "run_adaptive_influences",
                column: "source_type");

            migrationBuilder.CreateIndex(
                name: "IX_run_palace_indicator_snapshots_indicator_key",
                table: "run_palace_indicator_snapshots",
                column: "indicator_key");

            migrationBuilder.CreateIndex(
                name: "IX_run_palace_indicator_snapshots_run_id",
                table: "run_palace_indicator_snapshots",
                column: "run_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "run_adaptive_influences");

            migrationBuilder.DropTable(
                name: "run_palace_indicator_snapshots");
        }
    }
}
