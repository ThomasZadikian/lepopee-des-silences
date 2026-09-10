using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260910140000_AddDeveloperSandboxRunMode")]
public partial class AddDeveloperSandboxRunMode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_runs_player_active_or_suspended",
            table: "runs");

        migrationBuilder.AddColumn<string>(
            name: "mode",
            table: "runs",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Normal");

        migrationBuilder.CreateIndex(
            name: "ux_runs_player_mode_active_or_suspended",
            table: "runs",
            columns: new[] { "player_id", "mode" },
            unique: true,
            filter: "status IN ('Active', 'Suspended')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_runs_player_mode_active_or_suspended",
            table: "runs");

        migrationBuilder.DropColumn(
            name: "mode",
            table: "runs");

        migrationBuilder.CreateIndex(
            name: "ux_runs_player_active_or_suspended",
            table: "runs",
            column: "player_id",
            unique: true,
            filter: "status IN ('Active', 'Suspended')");
    }
}
