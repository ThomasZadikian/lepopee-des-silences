using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IntroduceDeterministicSelectionContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "run_selection_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    context_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    selected_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    seed = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    algorithm_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_selection_decisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_selection_decisions_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_run_selection_decisions_decision_type",
                table: "run_selection_decisions",
                column: "decision_type");

            migrationBuilder.CreateIndex(
                name: "IX_run_selection_decisions_run_id",
                table: "run_selection_decisions",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_selection_decisions_selected_key",
                table: "run_selection_decisions",
                column: "selected_key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "run_selection_decisions");
        }
    }
}
