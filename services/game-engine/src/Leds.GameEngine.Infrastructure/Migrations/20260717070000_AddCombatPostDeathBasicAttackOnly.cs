using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatPostDeathBasicAttackOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "post_death_basic_attack_only_enabled",
                table: "run_active_combats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "next_action_restricted_to_basic_attack",
                table: "run_active_combats",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "post_death_basic_attack_only_enabled",
                table: "run_active_combats");

            migrationBuilder.DropColumn(
                name: "next_action_restricted_to_basic_attack",
                table: "run_active_combats");
        }
    }
}
