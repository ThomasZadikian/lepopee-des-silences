using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260730200000_AddGroundLoot")]
public partial class AddGroundLoot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ground_room_id",
            table: "run_items",
            type: "uuid",
            nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "ground_x",
            table: "run_items",
            type: "integer",
            nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "ground_y",
            table: "run_items",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_run_items_ground_room_id_ground_x_ground_y",
            table: "run_items",
            columns: ["ground_room_id", "ground_x", "ground_y"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_run_items_ground_room_id_ground_x_ground_y",
            table: "run_items");
        migrationBuilder.DropColumn("ground_room_id", "run_items");
        migrationBuilder.DropColumn("ground_x", "run_items");
        migrationBuilder.DropColumn("ground_y", "run_items");
    }
}
