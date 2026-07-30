using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

public partial class AddRunItemTacticalContracts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "tactical_range",
            table: "run_items",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "tactical_area_shape",
            table: "run_items",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "Single");

        migrationBuilder.AddColumn<bool>(
            name: "requires_line_of_sight",
            table: "run_items",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "tactical_range", table: "run_items");
        migrationBuilder.DropColumn(name: "tactical_area_shape", table: "run_items");
        migrationBuilder.DropColumn(name: "requires_line_of_sight", table: "run_items");
    }
}
