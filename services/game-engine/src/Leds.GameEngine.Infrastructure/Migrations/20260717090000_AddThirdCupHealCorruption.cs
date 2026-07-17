using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddThirdCupHealCorruption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "third_cup_heal_corruption_enabled",
                table: "run_active_combats",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "third_cup_heal_corruption_enabled",
                table: "run_active_combats");
        }
    }
}
