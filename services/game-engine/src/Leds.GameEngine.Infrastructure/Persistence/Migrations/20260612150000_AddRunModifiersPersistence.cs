using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRunModifiersPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "run_modifiers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false),
                    duration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_modifiers", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_modifiers_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_run_modifiers_run_id",
                table: "run_modifiers",
                column: "run_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "run_modifiers");
        }
    }
}
