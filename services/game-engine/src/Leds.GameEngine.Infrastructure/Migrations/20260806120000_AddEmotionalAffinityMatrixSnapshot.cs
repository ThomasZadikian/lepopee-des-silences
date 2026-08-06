using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260806120000_AddEmotionalAffinityMatrixSnapshot")]
public sealed class AddEmotionalAffinityMatrixSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "emotional_affinity_matrix_version",
            table: "runs",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false);

        migrationBuilder.AddColumn<string>(
            name: "emotional_affinity_matrix_json",
            table: "runs",
            type: "text",
            nullable: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "emotional_affinity_matrix_version", table: "runs");
        migrationBuilder.DropColumn(name: "emotional_affinity_matrix_json", table: "runs");
    }
}
