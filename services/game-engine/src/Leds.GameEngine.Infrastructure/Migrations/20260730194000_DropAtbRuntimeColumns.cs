using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260730194000_DropAtbRuntimeColumns")]
public partial class DropAtbRuntimeColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "kind",
            table: "run_active_combats",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "Tactical",
            oldClrType: typeof(string),
            oldType: "character varying(16)",
            oldMaxLength: 16,
            oldDefaultValue: "Atb");

        migrationBuilder.DropColumn("atb_gauge_value", "run_combatant_runtime_states");
        migrationBuilder.DropColumn("action_recovery_until_tick", "run_combatant_runtime_states");
        migrationBuilder.DropColumn("atb_fill_per_tick", "run_combatant_runtime_states");
        migrationBuilder.DropColumn("atb_tempo_room_factor_per_mille", "run_combatant_runtime_states");
        migrationBuilder.DropColumn("atb_tempo_combatant_factor_per_mille", "run_combatant_runtime_states");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("atb_gauge_value", "run_combatant_runtime_states", nullable: true);
        migrationBuilder.AddColumn<int>("action_recovery_until_tick", "run_combatant_runtime_states", nullable: true);
        migrationBuilder.AddColumn<int>("atb_fill_per_tick", "run_combatant_runtime_states", nullable: true);
        migrationBuilder.AddColumn<int>("atb_tempo_room_factor_per_mille", "run_combatant_runtime_states", nullable: true);
        migrationBuilder.AddColumn<int>("atb_tempo_combatant_factor_per_mille", "run_combatant_runtime_states", nullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "kind",
            table: "run_active_combats",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "Atb",
            oldClrType: typeof(string),
            oldType: "character varying(16)",
            oldMaxLength: 16,
            oldDefaultValue: "Tactical");
    }
}
