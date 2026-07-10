using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillCategoryColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "run_player_skills",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Physical");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "run_combatant_skills",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Physical");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "run_player_skills");

            migrationBuilder.DropColumn(
                name: "category",
                table: "run_combatant_skills");
        }
    }
}
