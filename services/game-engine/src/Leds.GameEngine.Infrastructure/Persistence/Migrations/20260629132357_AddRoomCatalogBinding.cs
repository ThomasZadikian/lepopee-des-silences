using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomCatalogBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "catalog_display_name",
                table: "run_rooms",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "catalog_is_unique",
                table: "run_rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "catalog_narrative_text",
                table: "run_rooms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "catalog_room_key",
                table: "run_rooms",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "curse_pool_key",
                table: "run_rooms",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "enemy_pool_key",
                table: "run_rooms",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "law_pool_key",
                table: "run_rooms",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reward_pool_key",
                table: "run_rooms",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "catalog_display_name",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "catalog_is_unique",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "catalog_narrative_text",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "catalog_room_key",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "curse_pool_key",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "enemy_pool_key",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "law_pool_key",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "reward_pool_key",
                table: "run_rooms");
        }
    }
}
