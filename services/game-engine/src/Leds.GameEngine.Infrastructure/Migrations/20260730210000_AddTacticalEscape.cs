using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260730210000_AddTacticalEscape")]
public partial class AddTacticalEscape : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "tactical_escape_x",
            table: "run_active_combats",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "tactical_risk_tier",
            table: "run_active_combats",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "tactical_escape_y",
            table: "run_active_combats",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "tactical_equipped_items_csv",
            table: "run_active_combats",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "tactical_activation_counts_csv",
            table: "run_active_combats",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "tactical_last_magic_csv",
            table: "run_active_combats",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "tactical_cannot_revive_csv",
            table: "run_active_combats",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "tactical_escape_x", table: "run_active_combats");
        migrationBuilder.DropColumn(name: "tactical_escape_y", table: "run_active_combats");
        migrationBuilder.DropColumn(name: "tactical_risk_tier", table: "run_active_combats");
        migrationBuilder.DropColumn(name: "tactical_equipped_items_csv", table: "run_active_combats");
        migrationBuilder.DropColumn(name: "tactical_activation_counts_csv", table: "run_active_combats");
        migrationBuilder.DropColumn(name: "tactical_last_magic_csv", table: "run_active_combats");
        migrationBuilder.DropColumn(name: "tactical_cannot_revive_csv", table: "run_active_combats");
    }
}
