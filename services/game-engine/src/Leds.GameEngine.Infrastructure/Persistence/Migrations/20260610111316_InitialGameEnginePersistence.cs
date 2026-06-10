using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialGameEnginePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    seed = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    generator_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    markov_matrix_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    current_depth = table.Column<int>(type: "integer", nullable: false),
                    active_combat_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pending_reward_offer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_runs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_runs_created_at_utc",
                table: "runs",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_runs_player_id",
                table: "runs",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_runs_status",
                table: "runs",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "runs");
        }
    }
}
