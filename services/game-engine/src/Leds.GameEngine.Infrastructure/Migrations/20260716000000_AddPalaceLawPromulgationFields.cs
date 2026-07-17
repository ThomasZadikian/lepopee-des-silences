using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPalaceLawPromulgationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "last_promulgation_floor_index",
                table: "runs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "expires_at_floor_index",
                table: "run_modifiers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_promulgation_floor_index",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "expires_at_floor_index",
                table: "run_modifiers");
        }
    }
}
