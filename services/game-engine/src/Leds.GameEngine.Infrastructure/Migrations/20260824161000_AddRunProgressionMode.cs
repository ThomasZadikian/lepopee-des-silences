using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260824161000_AddRunProgressionMode")]
public partial class AddRunProgressionMode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("progression_mode", "runs", type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Standard");
        migrationBuilder.AddColumn<string>("story_difficulty", "runs", type: "character varying(32)", maxLength: 32, nullable: true);
        migrationBuilder.AddColumn<int>("difficulty_level", "runs", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>("story_sequence_key", "runs", type: "character varying(160)", maxLength: 160, nullable: true);
        migrationBuilder.AddColumn<string>("story_sequence_version", "runs", type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<string>("story_step_key", "runs", type: "character varying(160)", maxLength: 160, nullable: true);
        migrationBuilder.AddColumn<string>("story_checkpoint_key", "runs", type: "character varying(160)", maxLength: 160, nullable: true);

        migrationBuilder.AddCheckConstraint(
            "ck_runs_progression_mode",
            "runs",
            "(progression_mode = 'Story' AND story_difficulty IS NOT NULL AND difficulty_level IS NULL) OR " +
            "(progression_mode = 'Standard' AND story_difficulty IS NULL)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("ck_runs_progression_mode", "runs");
        migrationBuilder.DropColumn("progression_mode", "runs");
        migrationBuilder.DropColumn("story_difficulty", "runs");
        migrationBuilder.DropColumn("difficulty_level", "runs");
        migrationBuilder.DropColumn("story_sequence_key", "runs");
        migrationBuilder.DropColumn("story_sequence_version", "runs");
        migrationBuilder.DropColumn("story_step_key", "runs");
        migrationBuilder.DropColumn("story_checkpoint_key", "runs");
    }
}
