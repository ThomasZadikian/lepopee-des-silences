using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CatalogDbContext))]
[Migration("20260730230000_RemoveRecoveryStat")]
public partial class RemoveRecoveryStat : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("recovery", "catalog_enemy_stat_blocks");
        migrationBuilder.DropColumn("recovery_time", "catalog_skill_definitions");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            "recovery", "catalog_enemy_stat_blocks", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>(
            "recovery_time", "catalog_skill_definitions", nullable: false, defaultValue: 0);
    }
}
