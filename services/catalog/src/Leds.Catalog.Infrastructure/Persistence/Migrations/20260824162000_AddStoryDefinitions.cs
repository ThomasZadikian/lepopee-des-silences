using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CatalogDbContext))]
[Migration("20260824162000_AddStoryDefinitions")]
public partial class AddStoryDefinitions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "story_sequence_definitions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                entry_step_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_story_sequence_definitions", x => x.id));

        migrationBuilder.CreateTable(
            name: "story_step_definitions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                sequence_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                step_order = table.Column<int>(type: "integer", nullable: false),
                room_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                conditions_json = table.Column<string>(type: "text", nullable: false),
                effects_json = table.Column<string>(type: "text", nullable: false),
                is_terminal = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_story_step_definitions", x => x.id);
                table.ForeignKey(
                    "FK_story_step_definitions_story_sequence_definitions_sequence_definition_id",
                    x => x.sequence_definition_id,
                    "story_sequence_definitions",
                    "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_story_sequence_definitions_key", "story_sequence_definitions", "key", unique: true);
        migrationBuilder.CreateIndex("IX_story_step_definitions_sequence_definition_id_key", "story_step_definitions", new[] { "sequence_definition_id", "key" }, unique: true);
        migrationBuilder.CreateIndex("IX_story_step_definitions_sequence_definition_id_step_order", "story_step_definitions", new[] { "sequence_definition_id", "step_order" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("story_step_definitions");
        migrationBuilder.DropTable("story_sequence_definitions");
    }
}
