using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Player.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerCharacterSkillEquipmentSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE player_character_skills ADD COLUMN IF NOT EXISTS \"EquipmentSlot\" text NOT NULL DEFAULT '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE player_character_skills DROP COLUMN IF EXISTS \"EquipmentSlot\";");
        }
    }
}
