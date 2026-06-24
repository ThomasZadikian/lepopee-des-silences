using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatTypingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE runs ADD COLUMN IF NOT EXISTS focus integer NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE run_combatants ADD COLUMN IF NOT EXISTS attack_type_override integer NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE runs DROP COLUMN IF EXISTS focus;");
            migrationBuilder.Sql("ALTER TABLE run_combatants DROP COLUMN IF EXISTS attack_type_override;");
        }
    }
}