using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRunCombatMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "combat_mode",
                table: "runs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                // Les runs antérieures au choix de mode ont été jouées en ATB, seul système
                // existant alors : la valeur par défaut le dit explicitement plutôt que de
                // laisser une chaîne vide que la relecture devrait interpréter.
                defaultValue: "Atb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "combat_mode",
                table: "runs");
        }
    }
}
