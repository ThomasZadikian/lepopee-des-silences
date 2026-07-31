using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260731091000_AddTemporarySkillSlots")]
public partial class AddTemporarySkillSlots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<string>(
            "temporary_slot",
            "run_character_skill_snapshots",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "Permanent");

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn("temporary_slot", "run_character_skill_snapshots");
}
