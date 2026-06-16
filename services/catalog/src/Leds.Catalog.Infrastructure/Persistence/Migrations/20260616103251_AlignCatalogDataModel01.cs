using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignCatalogDataModel01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "key",
                table: "catalog_skill_definitions",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "effect_type",
                table: "catalog_skill_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "Legacy compatibility summary. Canonical effects live in catalog_effect_sets/catalog_effect_definitions.",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "catalog_skill_definitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AddColumn<int>(
                name: "accuracy",
                table: "catalog_skill_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "action_cost",
                table: "catalog_skill_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "base_weight",
                table: "catalog_skill_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "cast_time",
                table: "catalog_skill_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "cooldown",
                table: "catalog_skill_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "cost_amount",
                table: "catalog_skill_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "cost_type",
                table: "catalog_skill_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "catalog_skill_definitions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "effect_set_id",
                table: "catalog_skill_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "narrative_text",
                table: "catalog_skill_definitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "power",
                table: "catalog_skill_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "recovery_time",
                table: "catalog_skill_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "selection_group",
                table: "catalog_skill_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "targeting_mode",
                table: "catalog_skill_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "key",
                table: "catalog_palace_law_definitions",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "impact_domains_json",
                table: "catalog_palace_law_definitions",
                type: "text",
                nullable: false,
                comment: "Legacy JSON compatibility column. Structured effects/tags are relational in data-model-0.1.",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "catalog_palace_law_definitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AddColumn<int>(
                name: "base_weight",
                table: "catalog_palace_law_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "catalog_palace_law_definitions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "duration",
                table: "catalog_palace_law_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "UntilRunEnds");

            migrationBuilder.AddColumn<Guid>(
                name: "effect_set_id",
                table: "catalog_palace_law_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_depth",
                table: "catalog_palace_law_definitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "min_depth",
                table: "catalog_palace_law_definitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "narrative_text",
                table: "catalog_palace_law_definitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scope",
                table: "catalog_palace_law_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Run");

            migrationBuilder.AddColumn<string>(
                name: "selection_group",
                table: "catalog_palace_law_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "severity",
                table: "catalog_palace_law_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "trigger",
                table: "catalog_palace_law_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "key",
                table: "catalog_item_definitions",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<int>(
                name: "effect_value",
                table: "catalog_item_definitions",
                type: "integer",
                nullable: false,
                comment: "Legacy compatibility column. Canonical effects live in catalog_effect_sets/catalog_effect_definitions.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "duration",
                table: "catalog_item_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "Legacy compatibility column. Use lifecycle/usage_mode/effect_set_id for data-model-0.1 definitions.",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "catalog_item_definitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AddColumn<int>(
                name: "base_weight",
                table: "catalog_item_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "catalog_item_definitions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "effect_set_id",
                table: "catalog_item_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_usable_in_combat",
                table: "catalog_item_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_usable_outside_combat",
                table: "catalog_item_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "item_type",
                table: "catalog_item_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "lifecycle",
                table: "catalog_item_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "RuntimeRunOnly");

            migrationBuilder.AddColumn<int>(
                name: "max_depth",
                table: "catalog_item_definitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_stack",
                table: "catalog_item_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "min_depth",
                table: "catalog_item_definitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "narrative_text",
                table: "catalog_item_definitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "selection_group",
                table: "catalog_item_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stack_policy",
                table: "catalog_item_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Additive");

            migrationBuilder.AddColumn<string>(
                name: "usage_mode",
                table: "catalog_item_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "NotUsable");

            migrationBuilder.AlterColumn<string>(
                name: "tags_json",
                table: "catalog_enemy_definitions",
                type: "text",
                nullable: false,
                comment: "Legacy JSON compatibility column. Use catalog_enemy_tags for structured tags.",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "skill_keys_json",
                table: "catalog_enemy_definitions",
                type: "text",
                nullable: false,
                comment: "Legacy JSON compatibility column. Use catalog_enemy_skill_links for structured skill links.",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "key",
                table: "catalog_enemy_definitions",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "catalog_enemy_definitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AlterColumn<string>(
                name: "compatible_room_types_json",
                table: "catalog_enemy_definitions",
                type: "text",
                nullable: false,
                comment: "Legacy JSON compatibility column. Structured room pools/tags are relational in data-model-0.1.",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "base_weight",
                table: "catalog_enemy_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "catalog_enemy_definitions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "encounter_weight",
                table: "catalog_enemy_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "family",
                table: "catalog_enemy_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_boss",
                table: "catalog_enemy_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_elite",
                table: "catalog_enemy_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_depth",
                table: "catalog_enemy_definitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "min_depth",
                table: "catalog_enemy_definitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "narrative_text",
                table: "catalog_enemy_definitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rank",
                table: "catalog_enemy_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Common");

            migrationBuilder.AddColumn<string>(
                name: "reward_profile_key",
                table: "catalog_enemy_definitions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "catalog_enemy_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "selection_group",
                table: "catalog_enemy_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

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
                    charge = table.Column<int>(type: "integer", nullable: false)
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
                    enemy_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    danger_hint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    base_weight = table.Column<int>(type: "integer", nullable: false),
                    selection_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
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
                    effect_set_id = table.Column<Guid>(type: "uuid", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_catalog_skill_definitions_effect_set_id",
                table: "catalog_skill_definitions",
                column: "effect_set_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_palace_law_definitions_effect_set_id",
                table: "catalog_palace_law_definitions",
                column: "effect_set_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_definitions_effect_set_id",
                table: "catalog_item_definitions",
                column: "effect_set_id");

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
                name: "IX_catalog_enemy_stat_blocks_enemy_definition_id",
                table: "catalog_enemy_stat_blocks",
                column: "enemy_definition_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_tags_tag_id",
                table: "catalog_enemy_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_tags_tag_id",
                table: "catalog_item_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_law_tags_tag_id",
                table: "catalog_law_tags",
                column: "tag_id");

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
                name: "IX_catalog_room_type_definitions_key",
                table: "catalog_room_type_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_skill_tags_tag_id",
                table: "catalog_skill_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_tags_tag_key",
                table: "catalog_tags",
                column: "tag_key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_item_definitions_catalog_effect_sets_effect_set_id",
                table: "catalog_item_definitions",
                column: "effect_set_id",
                principalTable: "catalog_effect_sets",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_palace_law_definitions_catalog_effect_sets_effect_s~",
                table: "catalog_palace_law_definitions",
                column: "effect_set_id",
                principalTable: "catalog_effect_sets",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_skill_definitions_catalog_effect_sets_effect_set_id",
                table: "catalog_skill_definitions",
                column: "effect_set_id",
                principalTable: "catalog_effect_sets",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_item_definitions_catalog_effect_sets_effect_set_id",
                table: "catalog_item_definitions");

            migrationBuilder.DropForeignKey(
                name: "FK_catalog_palace_law_definitions_catalog_effect_sets_effect_s~",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropForeignKey(
                name: "FK_catalog_skill_definitions_catalog_effect_sets_effect_set_id",
                table: "catalog_skill_definitions");

            migrationBuilder.DropTable(
                name: "catalog_curse_tags");

            migrationBuilder.DropTable(
                name: "catalog_effect_definitions");

            migrationBuilder.DropTable(
                name: "catalog_enemy_skill_links");

            migrationBuilder.DropTable(
                name: "catalog_enemy_stat_blocks");

            migrationBuilder.DropTable(
                name: "catalog_enemy_tags");

            migrationBuilder.DropTable(
                name: "catalog_item_tags");

            migrationBuilder.DropTable(
                name: "catalog_law_tags");

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
                name: "catalog_room_reward_pool_entries");

            migrationBuilder.DropTable(
                name: "catalog_room_special_mechanics");

            migrationBuilder.DropTable(
                name: "catalog_room_tags");

            migrationBuilder.DropTable(
                name: "catalog_room_type_definitions");

            migrationBuilder.DropTable(
                name: "catalog_skill_tags");

            migrationBuilder.DropTable(
                name: "catalog_curse_definitions");

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
                name: "catalog_room_definitions");

            migrationBuilder.DropTable(
                name: "catalog_tags");

            migrationBuilder.DropTable(
                name: "catalog_effect_sets");

            migrationBuilder.DropIndex(
                name: "IX_catalog_skill_definitions_effect_set_id",
                table: "catalog_skill_definitions");

            migrationBuilder.DropIndex(
                name: "IX_catalog_palace_law_definitions_effect_set_id",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropIndex(
                name: "IX_catalog_item_definitions_effect_set_id",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "accuracy",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "action_cost",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "base_weight",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "cast_time",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "cooldown",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "cost_amount",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "cost_type",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "effect_set_id",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "narrative_text",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "power",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "recovery_time",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "selection_group",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "targeting_mode",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "base_weight",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "duration",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "effect_set_id",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "max_depth",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "min_depth",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "narrative_text",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "scope",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "selection_group",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "severity",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "trigger",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "base_weight",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "effect_set_id",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "is_usable_in_combat",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "is_usable_outside_combat",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "item_type",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "lifecycle",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "max_depth",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "max_stack",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "min_depth",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "narrative_text",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "selection_group",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "stack_policy",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "usage_mode",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "base_weight",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "encounter_weight",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "family",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "is_boss",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "is_elite",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "max_depth",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "min_depth",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "narrative_text",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "rank",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "reward_profile_key",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "role",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "selection_group",
                table: "catalog_enemy_definitions");

            migrationBuilder.AlterColumn<string>(
                name: "key",
                table: "catalog_skill_definitions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160);

            migrationBuilder.AlterColumn<string>(
                name: "effect_type",
                table: "catalog_skill_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "Legacy compatibility summary. Canonical effects live in catalog_effect_sets/catalog_effect_definitions.");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "catalog_skill_definitions",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "key",
                table: "catalog_palace_law_definitions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160);

            migrationBuilder.AlterColumn<string>(
                name: "impact_domains_json",
                table: "catalog_palace_law_definitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Legacy JSON compatibility column. Structured effects/tags are relational in data-model-0.1.");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "catalog_palace_law_definitions",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "key",
                table: "catalog_item_definitions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160);

            migrationBuilder.AlterColumn<int>(
                name: "effect_value",
                table: "catalog_item_definitions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Legacy compatibility column. Canonical effects live in catalog_effect_sets/catalog_effect_definitions.");

            migrationBuilder.AlterColumn<string>(
                name: "duration",
                table: "catalog_item_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "Legacy compatibility column. Use lifecycle/usage_mode/effect_set_id for data-model-0.1 definitions.");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "catalog_item_definitions",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "tags_json",
                table: "catalog_enemy_definitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Legacy JSON compatibility column. Use catalog_enemy_tags for structured tags.");

            migrationBuilder.AlterColumn<string>(
                name: "skill_keys_json",
                table: "catalog_enemy_definitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Legacy JSON compatibility column. Use catalog_enemy_skill_links for structured skill links.");

            migrationBuilder.AlterColumn<string>(
                name: "key",
                table: "catalog_enemy_definitions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "catalog_enemy_definitions",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "compatible_room_types_json",
                table: "catalog_enemy_definitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Legacy JSON compatibility column. Structured room pools/tags are relational in data-model-0.1.");
        }
    }
}
