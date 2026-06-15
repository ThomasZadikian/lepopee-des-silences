using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameEngineOutboxMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_engine_outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    event_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    causation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    destination = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_engine_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_engine_outbox_messages_occurred_at_utc",
                table: "game_engine_outbox_messages",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_game_engine_outbox_messages_processed_at_utc",
                table: "game_engine_outbox_messages",
                column: "processed_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_game_engine_outbox_messages_type",
                table: "game_engine_outbox_messages",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_engine_outbox_messages");
        }
    }
}