using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260824151000_MakeRoomBossOptional")]
public partial class MakeRoomBossOptional : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var (column, type, maxLength) in BossColumns)
        {
            migrationBuilder.AlterColumn<string>(
                name: column,
                table: "run_rooms",
                type: type,
                maxLength: maxLength,
                nullable: true,
                oldClrType: typeof(string),
                oldType: type,
                oldMaxLength: maxLength);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE run_rooms
            SET boss_id = 'legacy.missing',
                boss_name = 'Legacy missing boss',
                boss_room_type = room_type,
                boss_danger_hint = 'Unknown',
                boss_enemy_template_key = 'legacy.missing'
            WHERE boss_id IS NULL;
            """);

        foreach (var (column, type, maxLength) in BossColumns)
        {
            migrationBuilder.AlterColumn<string>(
                name: column,
                table: "run_rooms",
                type: type,
                maxLength: maxLength,
                nullable: false,
                oldClrType: typeof(string),
                oldType: type,
                oldMaxLength: maxLength,
                oldNullable: true);
        }
    }

    private static readonly (string Column, string Type, int MaxLength)[] BossColumns =
    [
        ("boss_id", "character varying(128)", 128),
        ("boss_name", "character varying(256)", 256),
        ("boss_room_type", "character varying(64)", 64),
        ("boss_danger_hint", "character varying(512)", 512),
        ("boss_enemy_template_key", "character varying(128)", 128)
    ];
}
