using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260730165500_AddCanonicalTacticalContracts")]
public partial class AddCanonicalTacticalContracts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("movement", "run_combatant_base_stat_snapshots", nullable: false, defaultValue: 4);

        AddSkillContract(migrationBuilder, "run_combatant_skills", includeCategoryMetadata: false);
        AddSkillContract(migrationBuilder, "run_player_skills", includeCategoryMetadata: false);
        AddSkillContract(migrationBuilder, "run_character_skill_snapshots", includeCategoryMetadata: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("movement", "run_combatant_base_stat_snapshots");
        DropSkillContract(migrationBuilder, "run_combatant_skills", includeCategoryMetadata: false);
        DropSkillContract(migrationBuilder, "run_player_skills", includeCategoryMetadata: false);
        DropSkillContract(migrationBuilder, "run_character_skill_snapshots", includeCategoryMetadata: true);
    }

    private static void AddSkillContract(
        MigrationBuilder migrationBuilder,
        string table,
        bool includeCategoryMetadata)
    {
        if (includeCategoryMetadata)
        {
            migrationBuilder.AddColumn<string>("category", table, maxLength: 16, nullable: false, defaultValue: "Physical");
            migrationBuilder.AddColumn<bool>("base_power_is_percent_of_max_vitality", table, nullable: false, defaultValue: false);
        }

        migrationBuilder.AddColumn<int>("tactical_range", table, nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<string>("tactical_area_shape", table, maxLength: 16, nullable: false, defaultValue: "Single");
        migrationBuilder.AddColumn<bool>("requires_line_of_sight", table, nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<int>("cooldown", table, nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<bool>("is_ultimate", table, nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>("emotional_register", table, maxLength: 32, nullable: false, defaultValue: "Neutral");
    }

    private static void DropSkillContract(
        MigrationBuilder migrationBuilder,
        string table,
        bool includeCategoryMetadata)
    {
        if (includeCategoryMetadata)
        {
            migrationBuilder.DropColumn("category", table);
            migrationBuilder.DropColumn("base_power_is_percent_of_max_vitality", table);
        }

        migrationBuilder.DropColumn("tactical_range", table);
        migrationBuilder.DropColumn("tactical_area_shape", table);
        migrationBuilder.DropColumn("requires_line_of_sight", table);
        migrationBuilder.DropColumn("cooldown", table);
        migrationBuilder.DropColumn("is_ultimate", table);
        migrationBuilder.DropColumn("emotional_register", table);
    }
}
