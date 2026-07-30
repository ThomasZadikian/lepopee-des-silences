using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260730230000_RemoveLegacyCombatSchema")]
public partial class RemoveLegacyCombatSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("kind", "run_active_combats");
        migrationBuilder.DropColumn("atb_ready_threshold", "run_combatant_base_stat_snapshots");
        migrationBuilder.DropColumn("recovery", "run_combatant_base_stat_snapshots");
        migrationBuilder.DropColumn("row", "run_combatants");
        migrationBuilder.DropColumn("atb_gauge_value", "run_combatant_runtime_states");
        migrationBuilder.DropColumn("action_recovery_until_tick", "run_combatant_runtime_states");
        migrationBuilder.DropColumn("atb_fill_per_tick", "run_combatant_runtime_states");
        migrationBuilder.DropColumn("atb_tempo_room_factor_per_mille", "run_combatant_runtime_states");
        migrationBuilder.DropColumn("atb_tempo_combatant_factor_per_mille", "run_combatant_runtime_states");
        migrationBuilder.DropColumn("tempo_momentum_per_mille", "run_combatant_runtime_states");
        migrationBuilder.DropColumn("recovery", "run_character_stat_snapshots");
        migrationBuilder.AlterColumn<decimal>(
            "charge", "run_combatants", type: "numeric(4,1)", nullable: false);
        migrationBuilder.AlterColumn<decimal>(
            "current_charge", "run_combatant_runtime_states", type: "numeric(4,1)", nullable: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            "kind", "run_active_combats", type: "character varying(16)",
            maxLength: 16, nullable: false, defaultValue: "Tactical");
        migrationBuilder.AddColumn<int?>(
            "atb_ready_threshold", "run_combatant_base_stat_snapshots", nullable: true);
        migrationBuilder.AddColumn<int>(
            "recovery", "run_combatant_base_stat_snapshots", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>(
            "row", "run_combatants", type: "character varying(16)",
            maxLength: 16, nullable: false, defaultValue: "Front");
        migrationBuilder.AddColumn<int?>(
            "atb_gauge_value", "run_combatant_runtime_states", nullable: true);
        migrationBuilder.AddColumn<int?>(
            "action_recovery_until_tick", "run_combatant_runtime_states", nullable: true);
        migrationBuilder.AddColumn<int?>(
            "atb_fill_per_tick", "run_combatant_runtime_states", nullable: true);
        migrationBuilder.AddColumn<int?>(
            "atb_tempo_room_factor_per_mille", "run_combatant_runtime_states", nullable: true);
        migrationBuilder.AddColumn<int?>(
            "atb_tempo_combatant_factor_per_mille", "run_combatant_runtime_states", nullable: true);
        migrationBuilder.AddColumn<int>(
            "tempo_momentum_per_mille", "run_combatant_runtime_states",
            nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>(
            "recovery", "run_character_stat_snapshots", nullable: false, defaultValue: 0);
        migrationBuilder.AlterColumn<int>(
            "charge", "run_combatants", type: "integer", nullable: false);
        migrationBuilder.AlterColumn<int>(
            "current_charge", "run_combatant_runtime_states", type: "integer", nullable: false);
    }
}
