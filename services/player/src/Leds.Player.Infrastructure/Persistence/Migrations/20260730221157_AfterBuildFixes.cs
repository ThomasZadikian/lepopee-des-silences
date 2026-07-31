using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Player.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AfterBuildFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EquipmentSlot",
                table: "player_character_skills",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EquipmentSlot",
                table: "player_character_skills");
        }
    }
}
