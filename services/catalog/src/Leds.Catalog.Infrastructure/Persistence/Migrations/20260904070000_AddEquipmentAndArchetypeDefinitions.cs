using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CatalogDbContext))]
[Migration("20260904070000_AddEquipmentAndArchetypeDefinitions")]
public partial class AddEquipmentAndArchetypeDefinitions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("allowed_slots_json", "catalog_item_definitions", "jsonb", nullable: false, defaultValue: "[]");
        migrationBuilder.AddColumn<string>("proficiency_tags_json", "catalog_item_definitions", "jsonb", nullable: false, defaultValue: "[]");
        migrationBuilder.AddColumn<string>("unique_equip_group", "catalog_item_definitions", "character varying(96)", maxLength: 96, nullable: true);

        migrationBuilder.CreateTable(
            "catalog_archetype_definitions",
            table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                description = table.Column<string>(type: "text", nullable: false),
                version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                base_stats_json = table.Column<string>(type: "jsonb", nullable: false),
                proficiency_tags_json = table.Column<string>(type: "jsonb", nullable: false),
                starter_equipment_json = table.Column<string>(type: "jsonb", nullable: false),
                starter_known_skills_json = table.Column<string>(type: "jsonb", nullable: false),
                starter_equipped_skills_json = table.Column<string>(type: "jsonb", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_catalog_archetype_definitions", x => x.id));
        migrationBuilder.CreateIndex("IX_catalog_archetype_definitions_key", "catalog_archetype_definitions", "key", unique: true);
        migrationBuilder.CreateIndex("IX_catalog_archetype_definitions_status", "catalog_archetype_definitions", "status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("catalog_archetype_definitions");
        migrationBuilder.DropColumn("allowed_slots_json", "catalog_item_definitions");
        migrationBuilder.DropColumn("proficiency_tags_json", "catalog_item_definitions");
        migrationBuilder.DropColumn("unique_equip_group", "catalog_item_definitions");
    }
}
