using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Player.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignPlayerDataModel01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "total_runs_started",
                table: "player_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "total_runs_failed",
                table: "player_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "total_runs_completed",
                table: "player_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "total_runs_abandoned",
                table: "player_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "auth_subject_id",
                table: "player_profiles",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "skill_keys_json",
                table: "player_characters",
                type: "text",
                nullable: false,
                comment: "Legacy compatibility column. Use player_character_skills for data-model-0.1.",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "max_vitality",
                table: "player_characters",
                type: "integer",
                nullable: false,
                comment: "Legacy compatibility column. Use player_character_stat_blocks.max_vitality for data-model-0.1.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "definition_key",
                table: "player_characters",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<int>(
                name: "base_mana",
                table: "player_characters",
                type: "integer",
                nullable: false,
                comment: "Legacy compatibility column. Use player_character_stat_blocks.mana for data-model-0.1.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "base_charge",
                table: "player_characters",
                type: "integer",
                nullable: false,
                comment: "Legacy compatibility column. Use player_character_stat_blocks.charge for data-model-0.1.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "character_type",
                table: "player_characters",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "player_characters",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "player_characters",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.Sql("""
                UPDATE player_characters
                SET updated_at_utc = created_at_utc;
                """);

            migrationBuilder.CreateTable(
                name: "player_character_skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    unlocked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_character_skills", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_character_skills_player_characters_player_character_~",
                        column: x => x.player_character_id,
                        principalTable: "player_characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_character_stat_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_vitality = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    attack_power = table.Column<int>(type: "integer", nullable: false, defaultValue: 12),
                    defense = table.Column<int>(type: "integer", nullable: false, defaultValue: 6),
                    starting_guard = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    speed = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    initiative = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    recovery = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    focus = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    mana = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    charge = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_character_stat_blocks", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_character_stat_blocks_player_characters_player_chara~",
                        column: x => x.player_character_id,
                        principalTable: "player_characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_permanent_unlocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unlock_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    unlock_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unlocked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_permanent_unlocks", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_permanent_unlocks_player_profiles_player_profile_id",
                        column: x => x.player_profile_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_run_statistics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seed = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    final_depth = table.Column<int>(type: "integer", nullable: false),
                    outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    generator_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ended_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    total_damage_dealt = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_damage_taken = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_guard_absorbed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_healing_done = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    combats_won = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    combats_lost = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    rewards_selected = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_items_used = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_run_statistics", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_run_statistics_player_profiles_player_profile_id",
                        column: x => x.player_profile_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO player_character_stat_blocks (
                    id,
                    player_character_id,
                    max_vitality,
                    attack_power,
                    defense,
                    starting_guard,
                    speed,
                    initiative,
                    recovery,
                    focus,
                    mana,
                    charge)
                SELECT
                    (substring(md5(c.id::text || ':stat-block'), 1, 8) || '-' ||
                     substring(md5(c.id::text || ':stat-block'), 9, 4) || '-' ||
                     substring(md5(c.id::text || ':stat-block'), 13, 4) || '-' ||
                     substring(md5(c.id::text || ':stat-block'), 17, 4) || '-' ||
                     substring(md5(c.id::text || ':stat-block'), 21, 12))::uuid,
                    c.id,
                    c.max_vitality,
                    12,
                    6,
                    0,
                    10,
                    10,
                    5,
                    0,
                    c.base_mana,
                    c.base_charge
                FROM player_characters c
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM player_character_stat_blocks existing
                    WHERE existing.player_character_id = c.id);
                """);

            migrationBuilder.Sql("""
                INSERT INTO player_character_skills (
                    id,
                    player_character_id,
                    skill_definition_key,
                    unlocked_at_utc,
                    source)
                SELECT
                    (substring(md5(c.id::text || ':' || skills.skill_definition_key), 1, 8) || '-' ||
                     substring(md5(c.id::text || ':' || skills.skill_definition_key), 9, 4) || '-' ||
                     substring(md5(c.id::text || ':' || skills.skill_definition_key), 13, 4) || '-' ||
                     substring(md5(c.id::text || ':' || skills.skill_definition_key), 17, 4) || '-' ||
                     substring(md5(c.id::text || ':' || skills.skill_definition_key), 21, 12))::uuid,
                    c.id,
                    skills.skill_definition_key,
                    c.created_at_utc,
                    'legacy'
                FROM player_characters c
                CROSS JOIN LATERAL (
                    SELECT DISTINCT value AS skill_definition_key
                    FROM jsonb_array_elements_text(
                        CASE
                            WHEN btrim(c.skill_keys_json) = '' THEN '[]'::jsonb
                            ELSE c.skill_keys_json::jsonb
                        END) AS value
                    WHERE btrim(value) <> ''
                ) skills
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM player_character_skills existing
                    WHERE existing.player_character_id = c.id
                      AND existing.skill_definition_key = skills.skill_definition_key);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_player_profiles_auth_subject_id",
                table: "player_profiles",
                column: "auth_subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_characters_player_profile_id_definition_key",
                table: "player_characters",
                columns: new[] { "player_profile_id", "definition_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_character_skills_player_character_id_skill_definitio~",
                table: "player_character_skills",
                columns: new[] { "player_character_id", "skill_definition_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_character_stat_blocks_player_character_id",
                table: "player_character_stat_blocks",
                column: "player_character_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_permanent_unlocks_player_profile_id_unlock_key",
                table: "player_permanent_unlocks",
                columns: new[] { "player_profile_id", "unlock_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_run_statistics_player_profile_id",
                table: "player_run_statistics",
                column: "player_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_run_statistics_run_id",
                table: "player_run_statistics",
                column: "run_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_character_skills");

            migrationBuilder.DropTable(
                name: "player_character_stat_blocks");

            migrationBuilder.DropTable(
                name: "player_permanent_unlocks");

            migrationBuilder.DropTable(
                name: "player_run_statistics");

            migrationBuilder.DropIndex(
                name: "IX_player_profiles_auth_subject_id",
                table: "player_profiles");

            migrationBuilder.DropIndex(
                name: "IX_player_characters_player_profile_id_definition_key",
                table: "player_characters");

            migrationBuilder.DropColumn(
                name: "auth_subject_id",
                table: "player_profiles");

            migrationBuilder.DropColumn(
                name: "character_type",
                table: "player_characters");

            migrationBuilder.DropColumn(
                name: "status",
                table: "player_characters");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "player_characters");

            migrationBuilder.AlterColumn<int>(
                name: "total_runs_started",
                table: "player_profiles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "total_runs_failed",
                table: "player_profiles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "total_runs_completed",
                table: "player_profiles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "total_runs_abandoned",
                table: "player_profiles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "skill_keys_json",
                table: "player_characters",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Legacy compatibility column. Use player_character_skills for data-model-0.1.");

            migrationBuilder.AlterColumn<int>(
                name: "max_vitality",
                table: "player_characters",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Legacy compatibility column. Use player_character_stat_blocks.max_vitality for data-model-0.1.");

            migrationBuilder.AlterColumn<string>(
                name: "definition_key",
                table: "player_characters",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160);

            migrationBuilder.AlterColumn<int>(
                name: "base_mana",
                table: "player_characters",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Legacy compatibility column. Use player_character_stat_blocks.mana for data-model-0.1.");

            migrationBuilder.AlterColumn<int>(
                name: "base_charge",
                table: "player_characters",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Legacy compatibility column. Use player_character_stat_blocks.charge for data-model-0.1.");
        }
    }
}
