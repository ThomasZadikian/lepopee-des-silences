using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialGameEngineBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_engine_outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    event_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    causation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    destination = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_engine_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "run_active_combats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    turn_number = table.Column<int>(type: "integer", nullable: false),
                    active_combatant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_active_combats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "run_combat_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    turn_number = table.Column<int>(type: "integer", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_side = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    skill_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    skill_display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    target_ids = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    source_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    source_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    raw_damage = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    mitigated_damage = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    vitality_damage = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    guard_damage = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    guard_absorbed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    guard_gained = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    damage_dealt = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    damage_taken = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    healing_done = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    healing_received = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    effects_applied = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_combat_actions", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_combat_actions_run_active_combats_combat_id",
                        column: x => x.combat_id,
                        principalTable: "run_active_combats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_combatants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    side = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    archetype = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    max_vitality = table.Column<int>(type: "integer", nullable: false),
                    current_vitality = table.Column<int>(type: "integer", nullable: false),
                    guard = table.Column<int>(type: "integer", nullable: false),
                    base_guard = table.Column<int>(type: "integer", nullable: false),
                    mana = table.Column<int>(type: "integer", nullable: false),
                    charge = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_combatants", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_combatants_run_active_combats_combat_id",
                        column: x => x.combat_id,
                        principalTable: "run_active_combats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    seed = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    generator_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    markov_matrix_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    current_room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_room_index = table.Column<int>(type: "integer", nullable: false),
                    active_combat_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pending_reward_offer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    max_hp = table.Column<int>(type: "integer", nullable: false),
                    current_hp = table.Column<int>(type: "integer", nullable: false),
                    attack = table.Column<int>(type: "integer", nullable: false),
                    defense = table.Column<int>(type: "integer", nullable: false),
                    speed = table.Column<int>(type: "integer", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    saved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pre_suspend_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    snapshot_current_hp = table.Column<int>(type: "integer", nullable: true),
                    snapshot_attack = table.Column<int>(type: "integer", nullable: true),
                    snapshot_defense = table.Column<int>(type: "integer", nullable: true),
                    snapshot_speed = table.Column<int>(type: "integer", nullable: true),
                    snapshot_memory_fragments = table.Column<string>(type: "text", nullable: true),
                    snapshot_active_palace_laws = table.Column<string>(type: "text", nullable: true),
                    snapshot_run_item_ids = table.Column<string>(type: "text", nullable: true),
                    snapshot_run_modifier_ids = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_runs_run_active_combats_active_combat_id",
                        column: x => x.active_combat_id,
                        principalTable: "run_active_combats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "run_combatant_base_stat_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combatant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_vitality = table.Column<int>(type: "integer", nullable: false),
                    attack_power = table.Column<int>(type: "integer", nullable: false),
                    defense = table.Column<int>(type: "integer", nullable: false),
                    starting_guard = table.Column<int>(type: "integer", nullable: false),
                    speed = table.Column<int>(type: "integer", nullable: false),
                    initiative = table.Column<int>(type: "integer", nullable: false),
                    recovery = table.Column<int>(type: "integer", nullable: false),
                    focus = table.Column<int>(type: "integer", nullable: false),
                    mana = table.Column<int>(type: "integer", nullable: false),
                    charge = table.Column<int>(type: "integer", nullable: false),
                    atb_ready_threshold = table.Column<int>(type: "integer", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_combatant_base_stat_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_combatant_base_stat_snapshots_run_combatants_combatant_~",
                        column: x => x.combatant_id,
                        principalTable: "run_combatants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_combatant_runtime_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combatant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_vitality = table.Column<int>(type: "integer", nullable: false),
                    current_guard = table.Column<int>(type: "integer", nullable: false),
                    current_focus = table.Column<int>(type: "integer", nullable: false),
                    current_mana = table.Column<int>(type: "integer", nullable: false),
                    current_charge = table.Column<int>(type: "integer", nullable: false),
                    atb_gauge_value = table.Column<int>(type: "integer", nullable: true),
                    action_recovery_until_tick = table.Column<int>(type: "integer", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_combatant_runtime_states", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_combatant_runtime_states_run_combatants_combatant_id",
                        column: x => x.combatant_id,
                        principalTable: "run_combatants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_combatant_skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combatant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    skill_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    targeting_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    effect_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    mana_cost = table.Column<int>(type: "integer", nullable: false),
                    charge_cost = table.Column<int>(type: "integer", nullable: false),
                    base_power = table.Column<int>(type: "integer", nullable: false),
                    tags = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_combatant_skills", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_combatant_skills_run_combatants_combatant_id",
                        column: x => x.combatant_id,
                        principalTable: "run_combatants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_active_curses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false, defaultValue: ""),
                    difficulty_delta = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    curse_definition_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    severity = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    duration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "NextCombatOnly"),
                    expires_at_room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consumed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effect_set_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    RunEntityId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_active_curses", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_active_curses_runs_RunEntityId",
                        column: x => x.RunEntityId,
                        principalTable: "runs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_run_active_curses_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_active_palace_laws",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    law_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    domains = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    duration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at_room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consumed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_active_palace_laws", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_active_palace_laws_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_adaptive_influences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    influence_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    influence_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    value_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    duration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_adaptive_influences", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_adaptive_influences_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    definition_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    narrative_text = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    rarity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    max_stack = table.Column<int>(type: "integer", nullable: true),
                    usage_mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    lifecycle = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    effect_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    effect_amount = table.Column<int>(type: "integer", nullable: false),
                    effect_set_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    effect_summary = table.Column<string>(type: "text", nullable: true),
                    is_usable_in_combat = table.Column<bool>(type: "boolean", nullable: true),
                    is_usable_outside_combat = table.Column<bool>(type: "boolean", nullable: true),
                    source_reward_option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_items_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_memory_fragments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fragment_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_memory_fragments", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_memory_fragments_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_modifiers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false),
                    duration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    value_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Flat"),
                    stack_policy = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Additive"),
                    expires_at_room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expires_at_combat_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_modifiers", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_modifiers_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_palace_indicator_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    indicator_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    narrative_text = table.Column<string>(type: "text", nullable: false),
                    intensity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_decision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_palace_indicator_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_palace_indicator_snapshots_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_player_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_player_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_player_snapshots_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_player_states",
                columns: table => new
                {
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_vitality = table.Column<int>(type: "integer", nullable: false),
                    current_vitality = table.Column<int>(type: "integer", nullable: false),
                    guard = table.Column<int>(type: "integer", nullable: false),
                    mana = table.Column<int>(type: "integer", nullable: false),
                    charge = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_player_states", x => x.run_id);
                    table.ForeignKey(
                        name: "FK_run_player_states_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_reward_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    combat_tier = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    difficulty_multiplier = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    reward_power_multiplier = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    catalog_reward_template_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    catalog_reward_template_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    selected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_reward_offers", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_reward_offers_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_rooms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false),
                    room_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    palace_state = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Neutral"),
                    theme = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    boss_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    boss_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    boss_room_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    boss_danger_hint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    boss_enemy_template_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    state = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    current_node_depth = table.Column<int>(type: "integer", nullable: false),
                    max_node_depth = table.Column<int>(type: "integer", nullable: false),
                    layout_template_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    layout_template_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_rooms", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_rooms_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_selection_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    context_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    selected_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    seed = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    algorithm_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    matrix_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    influence_summary_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_selection_decisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_selection_decisions_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_character_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_character_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_character_snapshots_run_player_snapshots_player_snapsho~",
                        column: x => x.player_snapshot_id,
                        principalTable: "run_player_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_player_skills",
                columns: table => new
                {
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    skill_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    targeting_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    effect_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    mana_cost = table.Column<int>(type: "integer", nullable: false),
                    charge_cost = table.Column<int>(type: "integer", nullable: false),
                    base_power = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_player_skills", x => new { x.run_id, x.key });
                    table.ForeignKey(
                        name: "FK_run_player_skills_run_player_states_run_id",
                        column: x => x.run_id,
                        principalTable: "run_player_states",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_reward_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reward_offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reward_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    payload_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    payload_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    effect_set_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    effect_set_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    base_amount = table.Column<int>(type: "integer", nullable: true),
                    scaled_amount = table.Column<int>(type: "integer", nullable: true),
                    is_selected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    selection_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_reward_options", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_reward_options_run_reward_offers_reward_offer_id",
                        column: x => x.reward_offer_id,
                        principalTable: "run_reward_offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    row = table.Column<int>(type: "integer", nullable: false),
                    lane = table.Column<int>(type: "integer", nullable: false),
                    risk_level = table.Column<int>(type: "integer", nullable: false),
                    reward_profile = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_boss = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    chosen_event_option_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_nodes", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_nodes_run_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "run_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_character_skill_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    character_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    skill_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    targeting_mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    effect_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    mana_cost = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    charge_cost = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    base_power = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_character_skill_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_character_skill_snapshots_run_character_snapshots_chara~",
                        column: x => x.character_snapshot_id,
                        principalTable: "run_character_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_character_stat_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    character_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_vitality = table.Column<int>(type: "integer", nullable: false),
                    attack_power = table.Column<int>(type: "integer", nullable: false),
                    defense = table.Column<int>(type: "integer", nullable: false),
                    starting_guard = table.Column<int>(type: "integer", nullable: false),
                    speed = table.Column<int>(type: "integer", nullable: false),
                    initiative = table.Column<int>(type: "integer", nullable: false),
                    recovery = table.Column<int>(type: "integer", nullable: false),
                    focus = table.Column<int>(type: "integer", nullable: false),
                    mana = table.Column<int>(type: "integer", nullable: false),
                    charge = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_character_stat_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_character_stat_snapshots_run_character_snapshots_charac~",
                        column: x => x.character_snapshot_id,
                        principalTable: "run_character_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_node_parent_nodes",
                columns: table => new
                {
                    map_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_node_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_node_parent_nodes", x => new { x.map_node_id, x.parent_node_id });
                    table.ForeignKey(
                        name: "FK_run_node_parent_nodes_run_nodes_map_node_id",
                        column: x => x.map_node_id,
                        principalTable: "run_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_engine_outbox_messages_occurred_at_utc",
                table: "game_engine_outbox_messages",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_game_engine_outbox_messages_processed_at_utc",
                table: "game_engine_outbox_messages",
                column: "processed_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_game_engine_outbox_messages_type",
                table: "game_engine_outbox_messages",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_run_active_combats_run_id",
                table: "run_active_combats",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_active_combats_status",
                table: "run_active_combats",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_run_active_curses_curse_definition_key",
                table: "run_active_curses",
                column: "curse_definition_key");

            migrationBuilder.CreateIndex(
                name: "IX_run_active_curses_run_id",
                table: "run_active_curses",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_active_curses_RunEntityId",
                table: "run_active_curses",
                column: "RunEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_run_active_palace_laws_key",
                table: "run_active_palace_laws",
                column: "key");

            migrationBuilder.CreateIndex(
                name: "IX_run_active_palace_laws_run_id",
                table: "run_active_palace_laws",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_adaptive_influences_influence_tag",
                table: "run_adaptive_influences",
                column: "influence_tag");

            migrationBuilder.CreateIndex(
                name: "IX_run_adaptive_influences_run_id",
                table: "run_adaptive_influences",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_adaptive_influences_source_type",
                table: "run_adaptive_influences",
                column: "source_type");

            migrationBuilder.CreateIndex(
                name: "IX_run_character_skill_snapshots_character_snapshot_id",
                table: "run_character_skill_snapshots",
                column: "character_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_character_skill_snapshots_skill_definition_key",
                table: "run_character_skill_snapshots",
                column: "skill_definition_key");

            migrationBuilder.CreateIndex(
                name: "IX_run_character_snapshots_player_snapshot_id",
                table: "run_character_snapshots",
                column: "player_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_character_stat_snapshots_character_snapshot_id",
                table: "run_character_stat_snapshots",
                column: "character_snapshot_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_run_combat_actions_actor_id",
                table: "run_combat_actions",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_combat_actions_combat_id",
                table: "run_combat_actions",
                column: "combat_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_combat_actions_occurred_at_utc",
                table: "run_combat_actions",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_run_combat_actions_turn_number",
                table: "run_combat_actions",
                column: "turn_number");

            migrationBuilder.CreateIndex(
                name: "IX_run_combatant_base_stat_snapshots_combatant_id",
                table: "run_combatant_base_stat_snapshots",
                column: "combatant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_run_combatant_runtime_states_combatant_id",
                table: "run_combatant_runtime_states",
                column: "combatant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_run_combatant_skills_combatant_id",
                table: "run_combatant_skills",
                column: "combatant_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_combatant_skills_key",
                table: "run_combatant_skills",
                column: "key");

            migrationBuilder.CreateIndex(
                name: "IX_run_combatants_combat_id",
                table: "run_combatants",
                column: "combat_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_combatants_side",
                table: "run_combatants",
                column: "side");

            migrationBuilder.CreateIndex(
                name: "IX_run_combatants_status",
                table: "run_combatants",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_run_items_definition_key",
                table: "run_items",
                column: "definition_key");

            migrationBuilder.CreateIndex(
                name: "IX_run_items_run_id",
                table: "run_items",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_items_source_reward_option_id",
                table: "run_items",
                column: "source_reward_option_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_memory_fragments_run_id",
                table: "run_memory_fragments",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_modifiers_run_id",
                table: "run_modifiers",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_node_parent_nodes_map_node_id",
                table: "run_node_parent_nodes",
                column: "map_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_node_parent_nodes_parent_node_id",
                table: "run_node_parent_nodes",
                column: "parent_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_nodes_room_id",
                table: "run_nodes",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_nodes_row",
                table: "run_nodes",
                column: "row");

            migrationBuilder.CreateIndex(
                name: "IX_run_nodes_state",
                table: "run_nodes",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "IX_run_palace_indicator_snapshots_indicator_key",
                table: "run_palace_indicator_snapshots",
                column: "indicator_key");

            migrationBuilder.CreateIndex(
                name: "IX_run_palace_indicator_snapshots_run_id",
                table: "run_palace_indicator_snapshots",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_player_snapshots_run_id",
                table: "run_player_snapshots",
                column: "run_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_run_reward_offers_run_id",
                table: "run_reward_offers",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_reward_offers_state",
                table: "run_reward_offers",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "IX_run_reward_options_reward_offer_id",
                table: "run_reward_options",
                column: "reward_offer_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_rooms_palace_state",
                table: "run_rooms",
                column: "palace_state");

            migrationBuilder.CreateIndex(
                name: "IX_run_rooms_run_id",
                table: "run_rooms",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_rooms_state",
                table: "run_rooms",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "IX_run_selection_decisions_decision_type",
                table: "run_selection_decisions",
                column: "decision_type");

            migrationBuilder.CreateIndex(
                name: "IX_run_selection_decisions_run_id",
                table: "run_selection_decisions",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_selection_decisions_selected_key",
                table: "run_selection_decisions",
                column: "selected_key");

            migrationBuilder.CreateIndex(
                name: "IX_runs_active_combat_id",
                table: "runs",
                column: "active_combat_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_runs_created_at_utc",
                table: "runs",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_runs_player_id",
                table: "runs",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_runs_status",
                table: "runs",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_engine_outbox_messages");

            migrationBuilder.DropTable(
                name: "run_active_curses");

            migrationBuilder.DropTable(
                name: "run_active_palace_laws");

            migrationBuilder.DropTable(
                name: "run_adaptive_influences");

            migrationBuilder.DropTable(
                name: "run_character_skill_snapshots");

            migrationBuilder.DropTable(
                name: "run_character_stat_snapshots");

            migrationBuilder.DropTable(
                name: "run_combat_actions");

            migrationBuilder.DropTable(
                name: "run_combatant_base_stat_snapshots");

            migrationBuilder.DropTable(
                name: "run_combatant_runtime_states");

            migrationBuilder.DropTable(
                name: "run_combatant_skills");

            migrationBuilder.DropTable(
                name: "run_items");

            migrationBuilder.DropTable(
                name: "run_memory_fragments");

            migrationBuilder.DropTable(
                name: "run_modifiers");

            migrationBuilder.DropTable(
                name: "run_node_parent_nodes");

            migrationBuilder.DropTable(
                name: "run_palace_indicator_snapshots");

            migrationBuilder.DropTable(
                name: "run_player_skills");

            migrationBuilder.DropTable(
                name: "run_reward_options");

            migrationBuilder.DropTable(
                name: "run_selection_decisions");

            migrationBuilder.DropTable(
                name: "run_character_snapshots");

            migrationBuilder.DropTable(
                name: "run_combatants");

            migrationBuilder.DropTable(
                name: "run_nodes");

            migrationBuilder.DropTable(
                name: "run_player_states");

            migrationBuilder.DropTable(
                name: "run_reward_offers");

            migrationBuilder.DropTable(
                name: "run_player_snapshots");

            migrationBuilder.DropTable(
                name: "run_rooms");

            migrationBuilder.DropTable(
                name: "runs");

            migrationBuilder.DropTable(
                name: "run_active_combats");
        }
    }
}
