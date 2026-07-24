using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_effect_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_effect_sets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_enemy_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    narrative_text = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    archetype = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    family = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    rank = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Common"),
                    role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    rarity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Common"),
                    registre = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    menace_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bound_room_keys_json = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    base_difficulty = table.Column<int>(type: "integer", nullable: false),
                    encounter_weight = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    min_risk_level = table.Column<int>(type: "integer", nullable: false),
                    max_risk_level = table.Column<int>(type: "integer", nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    is_boss = table.Column<bool>(type: "boolean", nullable: false),
                    is_elite = table.Column<bool>(type: "boolean", nullable: false),
                    reward_profile_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    base_weight = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    compatible_room_types_json = table.Column<string>(type: "text", nullable: false, comment: "Legacy JSON compatibility column. Structured room pools/tags are relational in data-model-0.1."),
                    tags_json = table.Column<string>(type: "text", nullable: false, comment: "Legacy JSON compatibility column. Use catalog_enemy_tags for structured tags."),
                    skill_keys_json = table.Column<string>(type: "text", nullable: false, comment: "Legacy JSON compatibility column. Use catalog_enemy_skill_links for structured skill links."),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_enemy_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_enemy_loot_tables",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    enemy_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    entries_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_enemy_loot_tables", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_enemy_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    archetype = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    element = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    max_health = table.Column<int>(type: "integer", nullable: false),
                    strength = table.Column<int>(type: "integer", nullable: false),
                    intelligence = table.Column<int>(type: "integer", nullable: false),
                    speed = table.Column<int>(type: "integer", nullable: false),
                    physical_resistance = table.Column<int>(type: "integer", nullable: false),
                    magical_resistance = table.Column<int>(type: "integer", nullable: false),
                    experience_reward = table.Column<int>(type: "integer", nullable: false),
                    gold_reward = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_enemy_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_event_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    default_outcome_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    min_risk_level = table.Column<int>(type: "integer", nullable: false),
                    max_risk_level = table.Column<int>(type: "integer", nullable: false),
                    requires_player_choice = table.Column<bool>(type: "boolean", nullable: false),
                    narrative_tags_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_event_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_generic_loot_pools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    entries_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_generic_loot_pools", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_npc_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    compatible_room_types_json = table.Column<string>(type: "text", nullable: false),
                    compatible_palace_room_states_json = table.Column<string>(type: "text", nullable: false),
                    compatible_room_climates_json = table.Column<string>(type: "text", nullable: false),
                    tags_json = table.Column<string>(type: "text", nullable: false),
                    emotional_affinity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Neutral"),
                    is_recurring = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    persona_json = table.Column<string>(type: "jsonb", nullable: true),
                    wounds_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    dialogue_graph_json = table.Column<string>(type: "jsonb", nullable: true),
                    encounter_keys_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    bound_room_keys_json = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    offerings_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_npc_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_npc_reputation_affinities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    npc_key_from = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    npc_key_to = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    weight = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_npc_reputation_affinities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_reward_curse_pools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    entries_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_reward_curse_pools", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_reward_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    source_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    min_options = table.Column<int>(type: "integer", nullable: false),
                    max_options = table.Column<int>(type: "integer", nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    base_weight = table.Column<int>(type: "integer", nullable: false),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_reward_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_boss_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    room_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    enemy_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    danger_hint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    base_difficulty = table.Column<int>(type: "integer", nullable: false),
                    base_weight = table.Column<int>(type: "integer", nullable: false),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tags_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_boss_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_curse_pools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_curse_pools", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_enemy_pools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_enemy_pools", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_law_pools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_law_pools", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_reward_pools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_reward_pools", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_theme_affinities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    theme_from = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    theme_to = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    weight = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_theme_affinities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_type_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    theme = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    default_rarity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_type_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_seed_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seed_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    checksum = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_seed_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_skill_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    element = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    effect_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    mana_cost = table.Column<int>(type: "integer", nullable: false),
                    charge_cost = table.Column<int>(type: "integer", nullable: false),
                    base_power = table.Column<int>(type: "integer", nullable: false),
                    heal_power = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_skill_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_curse_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    narrative_text = table.Column<string>(type: "text", nullable: true),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    duration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    trigger = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    effect_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    base_weight = table.Column<int>(type: "integer", nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_curse_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_curse_definitions_catalog_effect_sets_effect_set_id",
                        column: x => x.effect_set_id,
                        principalTable: "catalog_effect_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "catalog_effect_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    effect_set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effect_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_scope = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    value = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    value_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    duration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    stack_policy = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    condition = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    behavior_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    generation_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_effect_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_effect_definitions_catalog_effect_sets_effect_set_id",
                        column: x => x.effect_set_id,
                        principalTable: "catalog_effect_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_item_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    narrative_text = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    item_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    rarity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    usage_mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "NotUsable"),
                    lifecycle = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "RuntimeRunOnly"),
                    stack_policy = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Additive"),
                    max_stack = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_usable_in_combat = table.Column<bool>(type: "boolean", nullable: false),
                    is_usable_outside_combat = table.Column<bool>(type: "boolean", nullable: false),
                    effect_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    base_weight = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    duration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Legacy compatibility column. Use lifecycle/usage_mode/effect_set_id for data-model-0.1 definitions."),
                    effect_value = table.Column<int>(type: "integer", nullable: false, comment: "Legacy compatibility column. Canonical effects live in catalog_effect_sets/catalog_effect_definitions."),
                    effect_run_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    price = table.Column<int>(type: "integer", nullable: false),
                    equipment_effects_json = table.Column<string>(type: "jsonb", nullable: true),
                    is_container = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    container_capacity = table.Column<int>(type: "integer", nullable: true),
                    is_liquid = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    readable_pages_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_item_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_item_definitions_catalog_effect_sets_effect_set_id",
                        column: x => x.effect_set_id,
                        principalTable: "catalog_effect_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "catalog_palace_law_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    narrative_text = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    scope = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Run"),
                    duration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "UntilRunEnds"),
                    trigger = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    severity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    visibility = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    effect_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    base_weight = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    impact_domains_json = table.Column<string>(type: "text", nullable: false, comment: "Legacy JSON compatibility column. Structured effects/tags are relational in data-model-0.1."),
                    rarity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Commun"),
                    polarity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Neutre"),
                    is_majeure = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    room_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    is_cumul_exempt = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    exclusion_keys_json = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_palace_law_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_palace_law_definitions_catalog_effect_sets_effect_s~",
                        column: x => x.effect_set_id,
                        principalTable: "catalog_effect_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_special_mechanics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    mechanic_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    effect_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_special_mechanics", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_room_special_mechanics_catalog_effect_sets_effect_s~",
                        column: x => x.effect_set_id,
                        principalTable: "catalog_effect_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "catalog_skill_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    narrative_text = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    skill_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    targeting_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    targeting_mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    effect_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Legacy compatibility summary. Canonical effects live in catalog_effect_sets/catalog_effect_definitions."),
                    category = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "Physical"),
                    cost_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "None"),
                    cost_amount = table.Column<int>(type: "integer", nullable: false),
                    mana_cost = table.Column<int>(type: "integer", nullable: false),
                    charge_cost = table.Column<int>(type: "integer", nullable: false),
                    base_power = table.Column<int>(type: "integer", nullable: false),
                    base_power_is_percent_of_max_vitality = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    power = table.Column<int>(type: "integer", nullable: false),
                    accuracy = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    action_cost = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    cast_time = table.Column<int>(type: "integer", nullable: false),
                    recovery_time = table.Column<int>(type: "integer", nullable: false),
                    cooldown = table.Column<int>(type: "integer", nullable: false),
                    effect_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    base_weight = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    effects_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_skill_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_skill_definitions_catalog_effect_sets_effect_set_id",
                        column: x => x.effect_set_id,
                        principalTable: "catalog_effect_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "catalog_enemy_skill_links",
                columns: table => new
                {
                    enemy_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_enemy_skill_links", x => new { x.enemy_definition_id, x.skill_definition_key });
                    table.ForeignKey(
                        name: "FK_catalog_enemy_skill_links_catalog_enemy_definitions_enemy_d~",
                        column: x => x.enemy_definition_id,
                        principalTable: "catalog_enemy_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_enemy_stat_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enemy_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    magic_attack = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    magic_defense = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_enemy_stat_blocks", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_enemy_stat_blocks_catalog_enemy_definitions_enemy_d~",
                        column: x => x.enemy_definition_id,
                        principalTable: "catalog_enemy_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_reward_template_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reward_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reward_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    payload_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    base_amount = table.Column<int>(type: "integer", nullable: true),
                    scaling_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false),
                    effect_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    item_rarity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    item_effect_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_reward_template_options", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_reward_template_options_catalog_effect_sets_effect_~",
                        column: x => x.effect_set_id,
                        principalTable: "catalog_effect_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_catalog_reward_template_options_catalog_reward_templates_re~",
                        column: x => x.reward_template_id,
                        principalTable: "catalog_reward_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_curse_pool_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    curse_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    required_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    excluded_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_curse_pool_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_room_curse_pool_entries_catalog_room_curse_pools_po~",
                        column: x => x.pool_id,
                        principalTable: "catalog_room_curse_pools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_enemy_pool_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enemy_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    min_count = table.Column<int>(type: "integer", nullable: false),
                    max_count = table.Column<int>(type: "integer", nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    required_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    excluded_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_enemy_pool_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_room_enemy_pool_entries_catalog_room_enemy_pools_po~",
                        column: x => x.pool_id,
                        principalTable: "catalog_room_enemy_pools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_law_pool_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    law_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    required_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    excluded_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_law_pool_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_room_law_pool_entries_catalog_room_law_pools_pool_id",
                        column: x => x.pool_id,
                        principalTable: "catalog_room_law_pools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_reward_pool_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reward_template_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    item_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    weight = table.Column<int>(type: "integer", nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    required_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    excluded_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_reward_pool_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_room_reward_pool_entries_catalog_room_reward_pools_~",
                        column: x => x.pool_id,
                        principalTable: "catalog_room_reward_pools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_enemy_tags",
                columns: table => new
                {
                    enemy_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_enemy_tags", x => new { x.enemy_definition_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_catalog_enemy_tags_catalog_enemy_definitions_enemy_definiti~",
                        column: x => x.enemy_definition_id,
                        principalTable: "catalog_enemy_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_enemy_tags_catalog_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "catalog_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_reward_tags",
                columns: table => new
                {
                    reward_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_reward_tags", x => new { x.reward_template_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_catalog_reward_tags_catalog_reward_templates_reward_templat~",
                        column: x => x.reward_template_id,
                        principalTable: "catalog_reward_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_reward_tags_catalog_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "catalog_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_boss_tags",
                columns: table => new
                {
                    room_boss_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_boss_tags", x => new { x.room_boss_definition_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_catalog_room_boss_tags_catalog_room_boss_definitions_room_b~",
                        column: x => x.room_boss_definition_id,
                        principalTable: "catalog_room_boss_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_room_boss_tags_catalog_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "catalog_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_curse_tags",
                columns: table => new
                {
                    curse_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_curse_tags", x => new { x.curse_definition_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_catalog_curse_tags_catalog_curse_definitions_curse_definiti~",
                        column: x => x.curse_definition_id,
                        principalTable: "catalog_curse_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_curse_tags_catalog_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "catalog_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_item_tags",
                columns: table => new
                {
                    item_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_item_tags", x => new { x.item_definition_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_catalog_item_tags_catalog_item_definitions_item_definition_~",
                        column: x => x.item_definition_id,
                        principalTable: "catalog_item_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_item_tags_catalog_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "catalog_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_law_tags",
                columns: table => new
                {
                    palace_law_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_law_tags", x => new { x.palace_law_definition_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_catalog_law_tags_catalog_palace_law_definitions_palace_law_~",
                        column: x => x.palace_law_definition_id,
                        principalTable: "catalog_palace_law_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_law_tags_catalog_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "catalog_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_skill_tags",
                columns: table => new
                {
                    skill_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_skill_tags", x => new { x.skill_definition_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_catalog_skill_tags_catalog_skill_definitions_skill_definiti~",
                        column: x => x.skill_definition_id,
                        principalTable: "catalog_skill_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_skill_tags_catalog_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "catalog_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    narrative_text = table.Column<string>(type: "text", nullable: true),
                    room_family = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    room_rarity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    theme = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    base_weight = table.Column<int>(type: "integer", nullable: false),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    enemy_pool_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    reward_pool_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    law_pool_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    curse_pool_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    special_mechanic_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    boss_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    is_unique = table.Column<bool>(type: "boolean", nullable: false),
                    is_cultural_echo = table.Column<bool>(type: "boolean", nullable: false),
                    world_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reachability_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    triggers_strict_chain = table.Column<bool>(type: "boolean", nullable: false),
                    exclude_from_open_pool = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_reachability",
                columns: table => new
                {
                    from_room_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_room_definition_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_reachability", x => new { x.from_room_definition_id, x.to_room_definition_id });
                    table.ForeignKey(
                        name: "FK_catalog_room_reachability_catalog_room_definitions_from_roo~",
                        column: x => x.from_room_definition_id,
                        principalTable: "catalog_room_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_room_reachability_catalog_room_definitions_to_room_~",
                        column: x => x.to_room_definition_id,
                        principalTable: "catalog_room_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_tags",
                columns: table => new
                {
                    room_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_tags", x => new { x.room_definition_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_catalog_room_tags_catalog_room_definitions_room_definition_~",
                        column: x => x.room_definition_id,
                        principalTable: "catalog_room_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_room_tags_catalog_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "catalog_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_world_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    entry_room_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_world_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_world_definitions_catalog_room_definitions_entry_ro~",
                        column: x => x.entry_room_definition_id,
                        principalTable: "catalog_room_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_curse_definitions_effect_set_id",
                table: "catalog_curse_definitions",
                column: "effect_set_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_curse_definitions_key",
                table: "catalog_curse_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_curse_definitions_status",
                table: "catalog_curse_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_curse_tags_tag_id",
                table: "catalog_curse_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_effect_definitions_effect_set_id_order",
                table: "catalog_effect_definitions",
                columns: new[] { "effect_set_id", "order" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_effect_sets_key",
                table: "catalog_effect_sets",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_effect_sets_status",
                table: "catalog_effect_sets",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_definitions_key",
                table: "catalog_enemy_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_definitions_status",
                table: "catalog_enemy_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_loot_tables_enemy_definition_key",
                table: "catalog_enemy_loot_tables",
                column: "enemy_definition_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_loot_tables_key",
                table: "catalog_enemy_loot_tables",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_loot_tables_status",
                table: "catalog_enemy_loot_tables",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_stat_blocks_enemy_definition_id",
                table: "catalog_enemy_stat_blocks",
                column: "enemy_definition_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_tags_tag_id",
                table: "catalog_enemy_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_templates_key",
                table: "catalog_enemy_templates",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_templates_status",
                table: "catalog_enemy_templates",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_event_templates_key",
                table: "catalog_event_templates",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_event_templates_status",
                table: "catalog_event_templates",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_generic_loot_pools_key",
                table: "catalog_generic_loot_pools",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_generic_loot_pools_status",
                table: "catalog_generic_loot_pools",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_definitions_effect_set_id",
                table: "catalog_item_definitions",
                column: "effect_set_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_definitions_key",
                table: "catalog_item_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_definitions_status",
                table: "catalog_item_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_tags_tag_id",
                table: "catalog_item_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_law_tags_tag_id",
                table: "catalog_law_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_npc_definitions_key",
                table: "catalog_npc_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_npc_definitions_status",
                table: "catalog_npc_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_npc_reputation_affinities_npc_key_from_npc_key_to",
                table: "catalog_npc_reputation_affinities",
                columns: new[] { "npc_key_from", "npc_key_to" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_palace_law_definitions_effect_set_id",
                table: "catalog_palace_law_definitions",
                column: "effect_set_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_palace_law_definitions_key",
                table: "catalog_palace_law_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_palace_law_definitions_status",
                table: "catalog_palace_law_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_reward_curse_pools_key",
                table: "catalog_reward_curse_pools",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_reward_curse_pools_status",
                table: "catalog_reward_curse_pools",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_reward_tags_tag_id",
                table: "catalog_reward_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_reward_template_options_effect_set_id",
                table: "catalog_reward_template_options",
                column: "effect_set_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_reward_template_options_reward_template_id",
                table: "catalog_reward_template_options",
                column: "reward_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_reward_templates_key",
                table: "catalog_reward_templates",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_reward_templates_status",
                table: "catalog_reward_templates",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_boss_definitions_key",
                table: "catalog_room_boss_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_boss_definitions_status",
                table: "catalog_room_boss_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_boss_tags_tag_id",
                table: "catalog_room_boss_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_curse_pool_entries_pool_id",
                table: "catalog_room_curse_pool_entries",
                column: "pool_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_curse_pools_key",
                table: "catalog_room_curse_pools",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_curse_pools_status",
                table: "catalog_room_curse_pools",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_definitions_key",
                table: "catalog_room_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_definitions_status",
                table: "catalog_room_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_definitions_world_definition_id",
                table: "catalog_room_definitions",
                column: "world_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_enemy_pool_entries_pool_id",
                table: "catalog_room_enemy_pool_entries",
                column: "pool_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_enemy_pools_key",
                table: "catalog_room_enemy_pools",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_enemy_pools_status",
                table: "catalog_room_enemy_pools",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_law_pool_entries_pool_id",
                table: "catalog_room_law_pool_entries",
                column: "pool_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_law_pools_key",
                table: "catalog_room_law_pools",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_law_pools_status",
                table: "catalog_room_law_pools",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_reachability_to_room_definition_id",
                table: "catalog_room_reachability",
                column: "to_room_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_reward_pool_entries_pool_id",
                table: "catalog_room_reward_pool_entries",
                column: "pool_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_reward_pools_key",
                table: "catalog_room_reward_pools",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_reward_pools_status",
                table: "catalog_room_reward_pools",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_special_mechanics_effect_set_id",
                table: "catalog_room_special_mechanics",
                column: "effect_set_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_special_mechanics_key",
                table: "catalog_room_special_mechanics",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_tags_tag_id",
                table: "catalog_room_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_theme_affinities_theme_from_theme_to",
                table: "catalog_room_theme_affinities",
                columns: new[] { "theme_from", "theme_to" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_type_definitions_key",
                table: "catalog_room_type_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_seed_versions_seed_key_version",
                table: "catalog_seed_versions",
                columns: new[] { "seed_key", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_skill_definitions_effect_set_id",
                table: "catalog_skill_definitions",
                column: "effect_set_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_skill_definitions_key",
                table: "catalog_skill_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_skill_definitions_status",
                table: "catalog_skill_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_skill_tags_tag_id",
                table: "catalog_skill_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_skill_templates_key",
                table: "catalog_skill_templates",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_skill_templates_status",
                table: "catalog_skill_templates",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_tags_tag_key",
                table: "catalog_tags",
                column: "tag_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_world_definitions_entry_room_definition_id",
                table: "catalog_world_definitions",
                column: "entry_room_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_world_definitions_key",
                table: "catalog_world_definitions",
                column: "key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_room_definitions_catalog_world_definitions_world_de~",
                table: "catalog_room_definitions",
                column: "world_definition_id",
                principalTable: "catalog_world_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_room_definitions_catalog_world_definitions_world_de~",
                table: "catalog_room_definitions");

            migrationBuilder.DropTable(
                name: "catalog_curse_tags");

            migrationBuilder.DropTable(
                name: "catalog_effect_definitions");

            migrationBuilder.DropTable(
                name: "catalog_enemy_loot_tables");

            migrationBuilder.DropTable(
                name: "catalog_enemy_skill_links");

            migrationBuilder.DropTable(
                name: "catalog_enemy_stat_blocks");

            migrationBuilder.DropTable(
                name: "catalog_enemy_tags");

            migrationBuilder.DropTable(
                name: "catalog_enemy_templates");

            migrationBuilder.DropTable(
                name: "catalog_event_templates");

            migrationBuilder.DropTable(
                name: "catalog_generic_loot_pools");

            migrationBuilder.DropTable(
                name: "catalog_item_tags");

            migrationBuilder.DropTable(
                name: "catalog_law_tags");

            migrationBuilder.DropTable(
                name: "catalog_npc_definitions");

            migrationBuilder.DropTable(
                name: "catalog_npc_reputation_affinities");

            migrationBuilder.DropTable(
                name: "catalog_reward_curse_pools");

            migrationBuilder.DropTable(
                name: "catalog_reward_tags");

            migrationBuilder.DropTable(
                name: "catalog_reward_template_options");

            migrationBuilder.DropTable(
                name: "catalog_room_boss_tags");

            migrationBuilder.DropTable(
                name: "catalog_room_curse_pool_entries");

            migrationBuilder.DropTable(
                name: "catalog_room_enemy_pool_entries");

            migrationBuilder.DropTable(
                name: "catalog_room_law_pool_entries");

            migrationBuilder.DropTable(
                name: "catalog_room_reachability");

            migrationBuilder.DropTable(
                name: "catalog_room_reward_pool_entries");

            migrationBuilder.DropTable(
                name: "catalog_room_special_mechanics");

            migrationBuilder.DropTable(
                name: "catalog_room_tags");

            migrationBuilder.DropTable(
                name: "catalog_room_theme_affinities");

            migrationBuilder.DropTable(
                name: "catalog_room_type_definitions");

            migrationBuilder.DropTable(
                name: "catalog_seed_versions");

            migrationBuilder.DropTable(
                name: "catalog_skill_tags");

            migrationBuilder.DropTable(
                name: "catalog_skill_templates");

            migrationBuilder.DropTable(
                name: "catalog_curse_definitions");

            migrationBuilder.DropTable(
                name: "catalog_enemy_definitions");

            migrationBuilder.DropTable(
                name: "catalog_item_definitions");

            migrationBuilder.DropTable(
                name: "catalog_palace_law_definitions");

            migrationBuilder.DropTable(
                name: "catalog_reward_templates");

            migrationBuilder.DropTable(
                name: "catalog_room_boss_definitions");

            migrationBuilder.DropTable(
                name: "catalog_room_curse_pools");

            migrationBuilder.DropTable(
                name: "catalog_room_enemy_pools");

            migrationBuilder.DropTable(
                name: "catalog_room_law_pools");

            migrationBuilder.DropTable(
                name: "catalog_room_reward_pools");

            migrationBuilder.DropTable(
                name: "catalog_skill_definitions");

            migrationBuilder.DropTable(
                name: "catalog_tags");

            migrationBuilder.DropTable(
                name: "catalog_effect_sets");

            migrationBuilder.DropTable(
                name: "catalog_world_definitions");

            migrationBuilder.DropTable(
                name: "catalog_room_definitions");
        }
    }
}
