using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Player.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentAndPermanentBackpack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_character_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    acquired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_equipped = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_character_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_character_items_player_characters_player_character_id",
                        column: x => x.player_character_id,
                        principalTable: "player_characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_permanent_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    source_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    acquired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_permanent_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_permanent_items_player_profiles_player_profile_id",
                        column: x => x.player_profile_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_character_items_player_character_id_item_definitio~",
                table: "player_character_items",
                columns: new[] { "player_character_id", "item_definition_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_permanent_items_player_profile_id_item_definition_k~",
                table: "player_permanent_items",
                columns: new[] { "player_profile_id", "item_definition_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_character_items");

            migrationBuilder.DropTable(
                name: "player_permanent_items");
        }
    }
}
