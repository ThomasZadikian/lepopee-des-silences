using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Player.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialV1_0_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_processed_integration_events",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_processed_integration_events", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "player_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auth_subject_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    total_runs_started = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_runs_completed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_runs_failed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_runs_abandoned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    unspent_stat_points = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_stat_points_earned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    palace_shard_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    him_lit_shard_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    main_story_sequence_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    main_story_sequence_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    main_story_step_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    main_story_checkpoint_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    main_story_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    highest_difficulty_level_unlocked = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    main_story_unlocked_room_keys_json = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    main_story_visible_room_keys_json = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "account_identities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    email_verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    mfa_secret_protected = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    mfa_configured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    recovery_code_hashes_json = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    closure_requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closure_execute_after_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closure_cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_identities", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_identities_player_profiles_account_id",
                        column: x => x.account_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_privacy_consents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    policy_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_privacy_consents", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_privacy_consents_player_profiles_account_id",
                        column: x => x.account_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_security_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_security_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_security_tokens_player_profiles_account_id",
                        column: x => x.account_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_sessions",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refresh_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    rotated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_sessions", x => x.session_id);
                    table.ForeignKey(
                        name: "FK_account_sessions_player_profiles_account_id",
                        column: x => x.account_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "active_game_session_leases",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acquired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_active_game_session_leases", x => x.account_id);
                    table.ForeignKey(
                        name: "FK_active_game_session_leases_player_profiles_account_id",
                        column: x => x.account_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_characters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    character_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Standard"),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Active"),
                    archetype_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    archived_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    max_vitality = table.Column<int>(type: "integer", nullable: false, comment: "Legacy compatibility column. Use player_character_stat_blocks.max_vitality for data-model-0.1."),
                    base_mana = table.Column<int>(type: "integer", nullable: false, comment: "Legacy compatibility column. Use player_character_stat_blocks.mana for data-model-0.1."),
                    base_charge = table.Column<int>(type: "integer", nullable: false, comment: "Legacy compatibility column. Use player_character_stat_blocks.charge for data-model-0.1."),
                    stat_points_invested = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    skill_keys_json = table.Column<string>(type: "text", nullable: false, comment: "Legacy compatibility column. Use player_character_skills for data-model-0.1."),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_characters", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_characters_player_profiles_player_profile_id",
                        column: x => x.player_profile_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_npc_reputation_scores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    npc_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    times_met = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    current_dialogue_node_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_npc_reputation_scores", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_npc_reputation_scores_player_profiles_player_profile~",
                        column: x => x.player_profile_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_permanent_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    source_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    acquired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    contained_liquid_definition_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_permanent_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_permanent_items_player_profiles_player_profile_id",
                        column: x => x.player_profile_id,
                        principalTable: "player_profiles",
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

            migrationBuilder.CreateTable(
                name: "player_character_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    acquired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_equipped = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    equipment_slot = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "Relic")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_character_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_character_items_player_characters_player_character_id",
                        column: x => x.player_character_id,
                        principalTable: "player_characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_character_skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    unlocked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_equipped = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EquipmentSlot = table.Column<string>(type: "text", nullable: false)
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
                    focus = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    mana = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    charge = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    magic_attack = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    magic_defense = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    movement = table.Column<int>(type: "integer", nullable: false, defaultValue: 4)
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

            migrationBuilder.CreateIndex(
                name: "IX_account_identities_account_id",
                table: "account_identities",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_identities_email",
                table: "account_identities",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_privacy_consents_account_id_purpose_key_granted_at_~",
                table: "account_privacy_consents",
                columns: new[] { "account_id", "purpose_key", "granted_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_account_security_tokens_account_id",
                table: "account_security_tokens",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_security_tokens_purpose_token_hash",
                table: "account_security_tokens",
                columns: new[] { "purpose", "token_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_sessions_account_id",
                table: "account_sessions",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_character_items_player_character_id_item_definition_~",
                table: "player_character_items",
                columns: new[] { "player_character_id", "item_definition_key" },
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
                name: "IX_player_characters_player_profile_id",
                table: "player_characters",
                column: "player_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_characters_player_profile_id_definition_key",
                table: "player_characters",
                columns: new[] { "player_profile_id", "definition_key" });

            migrationBuilder.CreateIndex(
                name: "IX_player_npc_reputation_scores_player_profile_id_npc_key",
                table: "player_npc_reputation_scores",
                columns: new[] { "player_profile_id", "npc_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_permanent_items_player_profile_id_item_definition_key",
                table: "player_permanent_items",
                columns: new[] { "player_profile_id", "item_definition_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_permanent_unlocks_player_profile_id_unlock_key",
                table: "player_permanent_unlocks",
                columns: new[] { "player_profile_id", "unlock_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_processed_integration_events_processed_at_utc",
                table: "player_processed_integration_events",
                column: "processed_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_player_profiles_auth_subject_id",
                table: "player_profiles",
                column: "auth_subject_id");

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
                name: "account_identities");

            migrationBuilder.DropTable(
                name: "account_privacy_consents");

            migrationBuilder.DropTable(
                name: "account_security_tokens");

            migrationBuilder.DropTable(
                name: "account_sessions");

            migrationBuilder.DropTable(
                name: "active_game_session_leases");

            migrationBuilder.DropTable(
                name: "player_character_items");

            migrationBuilder.DropTable(
                name: "player_character_skills");

            migrationBuilder.DropTable(
                name: "player_character_stat_blocks");

            migrationBuilder.DropTable(
                name: "player_npc_reputation_scores");

            migrationBuilder.DropTable(
                name: "player_permanent_items");

            migrationBuilder.DropTable(
                name: "player_permanent_unlocks");

            migrationBuilder.DropTable(
                name: "player_processed_integration_events");

            migrationBuilder.DropTable(
                name: "player_run_statistics");

            migrationBuilder.DropTable(
                name: "player_characters");

            migrationBuilder.DropTable(
                name: "player_profiles");
        }
    }
}
