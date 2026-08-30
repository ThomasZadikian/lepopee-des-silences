using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CatalogDbContext))]
[Migration("20260805120000_AddSkillAudienceAndArchetypeRestriction")]
public partial class AddSkillAudienceAndArchetypeRestriction : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("audience", "catalog_skill_definitions", maxLength: 16, nullable: false, defaultValue: "Player");
        migrationBuilder.AddColumn<string>("allowed_archetypes_json", "catalog_skill_definitions", type: "jsonb", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("audience", "catalog_skill_definitions");
        migrationBuilder.DropColumn("allowed_archetypes_json", "catalog_skill_definitions");
    }
}
