using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActivePalaceLawPromulgationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rarity",
                table: "run_active_palace_laws",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Commun");

            migrationBuilder.AddColumn<string>(
                name: "polarity",
                table: "run_active_palace_laws",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Neutre");

            migrationBuilder.AddColumn<bool>(
                name: "is_majeure",
                table: "run_active_palace_laws",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "room_key",
                table: "run_active_palace_laws",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_cumul_exempt",
                table: "run_active_palace_laws",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rarity",
                table: "run_active_palace_laws");

            migrationBuilder.DropColumn(
                name: "polarity",
                table: "run_active_palace_laws");

            migrationBuilder.DropColumn(
                name: "is_majeure",
                table: "run_active_palace_laws");

            migrationBuilder.DropColumn(
                name: "room_key",
                table: "run_active_palace_laws");

            migrationBuilder.DropColumn(
                name: "is_cumul_exempt",
                table: "run_active_palace_laws");
        }
    }
}
