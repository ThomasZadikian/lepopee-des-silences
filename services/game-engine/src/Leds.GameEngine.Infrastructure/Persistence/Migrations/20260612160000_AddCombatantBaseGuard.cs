using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCombatantBaseGuard : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "base_guard",
            table: "run_combatants",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "base_guard",
            table: "run_combatants");
    }
}