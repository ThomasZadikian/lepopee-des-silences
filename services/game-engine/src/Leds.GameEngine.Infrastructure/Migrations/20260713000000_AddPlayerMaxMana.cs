using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerMaxMana : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_mana",
                table: "run_player_states",
                type: "integer",
                nullable: false,
                defaultValue: int.MaxValue);

            migrationBuilder.AddColumn<int>(
                name: "max_mana",
                table: "run_combatants",
                type: "integer",
                nullable: false,
                defaultValue: int.MaxValue);

            migrationBuilder.AddColumn<int>(
                name: "max_mana",
                table: "run_combatant_runtime_states",
                type: "integer",
                nullable: false,
                defaultValue: int.MaxValue);

            migrationBuilder.AddColumn<int>(
                name: "healing_bonus_percent",
                table: "runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "healing_bonus_percent",
                table: "run_combatants",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "healing_bonus_percent",
                table: "run_combatants");

            migrationBuilder.DropColumn(
                name: "healing_bonus_percent",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "max_mana",
                table: "run_combatant_runtime_states");

            migrationBuilder.DropColumn(
                name: "max_mana",
                table: "run_combatants");

            migrationBuilder.DropColumn(
                name: "max_mana",
                table: "run_player_states");
        }
    }
}
