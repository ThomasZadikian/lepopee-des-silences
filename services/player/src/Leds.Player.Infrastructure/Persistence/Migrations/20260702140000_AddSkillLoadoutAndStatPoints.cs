using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Player.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillLoadoutAndStatPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_equipped",
                table: "player_character_skills",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Backfill: every existing character has at most 4 known skills
            // today, so marking them all equipped keeps current players
            // combat-ready without violating the 4-skill equip cap.
            migrationBuilder.Sql("UPDATE player_character_skills SET is_equipped = true;");

            migrationBuilder.AddColumn<int>(
                name: "unspent_stat_points",
                table: "player_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "total_stat_points_earned",
                table: "player_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_equipped",
                table: "player_character_skills");

            migrationBuilder.DropColumn(
                name: "unspent_stat_points",
                table: "player_profiles");

            migrationBuilder.DropColumn(
                name: "total_stat_points_earned",
                table: "player_profiles");
        }
    }
}
