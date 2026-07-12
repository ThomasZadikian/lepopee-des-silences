using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Player.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerNpcReputationScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_npc_reputation_scores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    npc_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    times_met = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    current_dialogue_node_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_npc_reputation_scores", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_npc_reputation_scores_player_profiles_player_profile~",
                        column: x => x.player_profile_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_npc_reputation_scores_player_profile_id_npc_key",
                table: "player_npc_reputation_scores",
                columns: new[] { "player_profile_id", "npc_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_npc_reputation_scores");
        }
    }
}
