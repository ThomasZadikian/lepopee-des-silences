CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE player_processed_integration_events (
    event_id uuid NOT NULL,
    type character varying(128) NOT NULL,
    processed_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT "PK_player_processed_integration_events" PRIMARY KEY (event_id)
);

CREATE TABLE player_profiles (
    id uuid NOT NULL,
    auth_subject_id character varying(160),
    display_name character varying(128) NOT NULL,
    total_runs_started integer NOT NULL DEFAULT 0,
    total_runs_completed integer NOT NULL DEFAULT 0,
    total_runs_failed integer NOT NULL DEFAULT 0,
    total_runs_abandoned integer NOT NULL DEFAULT 0,
    unspent_stat_points integer NOT NULL DEFAULT 0,
    total_stat_points_earned integer NOT NULL DEFAULT 0,
    palace_shard_count integer NOT NULL DEFAULT 0,
    him_lit_shard_count integer NOT NULL DEFAULT 0,
    main_story_sequence_key character varying(160),
    main_story_sequence_version character varying(64),
    main_story_step_key character varying(160),
    main_story_checkpoint_key character varying(160),
    main_story_completed boolean NOT NULL DEFAULT FALSE,
    highest_difficulty_level_unlocked integer NOT NULL DEFAULT 0,
    main_story_unlocked_room_keys_json text NOT NULL DEFAULT '[]',
    main_story_visible_room_keys_json text NOT NULL DEFAULT '[]',
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT "PK_player_profiles" PRIMARY KEY (id)
);

CREATE TABLE account_identities (
    id uuid NOT NULL,
    account_id uuid NOT NULL,
    email character varying(320) NOT NULL,
    password_hash character varying(1024) NOT NULL,
    role integer NOT NULL DEFAULT 0,
    created_at_utc timestamp with time zone NOT NULL,
    email_verified_at_utc timestamp with time zone,
    mfa_secret_protected character varying(2048),
    mfa_configured_at_utc timestamp with time zone,
    recovery_code_hashes_json text NOT NULL DEFAULT '[]',
    closure_requested_at_utc timestamp with time zone,
    closure_execute_after_utc timestamp with time zone,
    closure_cancelled_at_utc timestamp with time zone,
    CONSTRAINT "PK_account_identities" PRIMARY KEY (id),
    CONSTRAINT "FK_account_identities_player_profiles_account_id" FOREIGN KEY (account_id) REFERENCES player_profiles (id) ON DELETE CASCADE
);

CREATE TABLE account_privacy_consents (
    id uuid NOT NULL,
    account_id uuid NOT NULL,
    purpose_key character varying(128) NOT NULL,
    policy_version character varying(64) NOT NULL,
    granted_at_utc timestamp with time zone NOT NULL,
    revoked_at_utc timestamp with time zone,
    CONSTRAINT "PK_account_privacy_consents" PRIMARY KEY (id),
    CONSTRAINT "FK_account_privacy_consents_player_profiles_account_id" FOREIGN KEY (account_id) REFERENCES player_profiles (id) ON DELETE CASCADE
);

CREATE TABLE account_security_tokens (
    id uuid NOT NULL,
    account_id uuid NOT NULL,
    purpose character varying(64) NOT NULL,
    token_hash character varying(128) NOT NULL,
    issued_at_utc timestamp with time zone NOT NULL,
    expires_at_utc timestamp with time zone NOT NULL,
    consumed_at_utc timestamp with time zone,
    CONSTRAINT "PK_account_security_tokens" PRIMARY KEY (id),
    CONSTRAINT "FK_account_security_tokens_player_profiles_account_id" FOREIGN KEY (account_id) REFERENCES player_profiles (id) ON DELETE CASCADE
);

CREATE TABLE account_sessions (
    session_id uuid NOT NULL,
    account_id uuid NOT NULL,
    refresh_token_hash character varying(128) NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    expires_at_utc timestamp with time zone NOT NULL,
    rotated_at_utc timestamp with time zone,
    revoked_at_utc timestamp with time zone,
    CONSTRAINT "PK_account_sessions" PRIMARY KEY (session_id),
    CONSTRAINT "FK_account_sessions_player_profiles_account_id" FOREIGN KEY (account_id) REFERENCES player_profiles (id) ON DELETE CASCADE
);

CREATE TABLE active_game_session_leases (
    account_id uuid NOT NULL,
    owner_session_id uuid NOT NULL,
    acquired_at_utc timestamp with time zone NOT NULL,
    expires_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT "PK_active_game_session_leases" PRIMARY KEY (account_id),
    CONSTRAINT "FK_active_game_session_leases_player_profiles_account_id" FOREIGN KEY (account_id) REFERENCES player_profiles (id) ON DELETE CASCADE
);

CREATE TABLE player_characters (
    id uuid NOT NULL,
    player_profile_id uuid NOT NULL,
    definition_key character varying(160) NOT NULL,
    display_name character varying(256) NOT NULL,
    character_type character varying(64) NOT NULL DEFAULT 'Standard',
    status character varying(32) NOT NULL DEFAULT 'Active',
    archetype_key character varying(160),
    archived_at_utc timestamp with time zone,
    max_vitality integer NOT NULL,
    base_mana integer NOT NULL,
    base_charge integer NOT NULL,
    stat_points_invested integer NOT NULL DEFAULT 0,
    skill_keys_json text NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT "PK_player_characters" PRIMARY KEY (id),
    CONSTRAINT "FK_player_characters_player_profiles_player_profile_id" FOREIGN KEY (player_profile_id) REFERENCES player_profiles (id) ON DELETE CASCADE
);
COMMENT ON COLUMN player_characters.max_vitality IS 'Legacy compatibility column. Use player_character_stat_blocks.max_vitality for data-model-0.1.';
COMMENT ON COLUMN player_characters.base_mana IS 'Legacy compatibility column. Use player_character_stat_blocks.mana for data-model-0.1.';
COMMENT ON COLUMN player_characters.base_charge IS 'Legacy compatibility column. Use player_character_stat_blocks.charge for data-model-0.1.';
COMMENT ON COLUMN player_characters.skill_keys_json IS 'Legacy compatibility column. Use player_character_skills for data-model-0.1.';

CREATE TABLE player_npc_reputation_scores (
    id uuid NOT NULL,
    player_profile_id uuid NOT NULL,
    npc_key character varying(128) NOT NULL,
    score integer NOT NULL DEFAULT 0,
    times_met integer NOT NULL DEFAULT 0,
    current_dialogue_node_key character varying(256),
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT "PK_player_npc_reputation_scores" PRIMARY KEY (id),
    CONSTRAINT "FK_player_npc_reputation_scores_player_profiles_player_profile~" FOREIGN KEY (player_profile_id) REFERENCES player_profiles (id) ON DELETE CASCADE
);

CREATE TABLE player_permanent_items (
    id uuid NOT NULL,
    player_profile_id uuid NOT NULL,
    item_definition_key character varying(160) NOT NULL,
    source_run_id uuid,
    acquired_at_utc timestamp with time zone NOT NULL,
    contained_liquid_definition_key character varying(256),
    CONSTRAINT "PK_player_permanent_items" PRIMARY KEY (id),
    CONSTRAINT "FK_player_permanent_items_player_profiles_player_profile_id" FOREIGN KEY (player_profile_id) REFERENCES player_profiles (id) ON DELETE CASCADE
);

CREATE TABLE player_permanent_unlocks (
    id uuid NOT NULL,
    player_profile_id uuid NOT NULL,
    unlock_key character varying(160) NOT NULL,
    unlock_type character varying(64) NOT NULL,
    source_run_id uuid,
    unlocked_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT "PK_player_permanent_unlocks" PRIMARY KEY (id),
    CONSTRAINT "FK_player_permanent_unlocks_player_profiles_player_profile_id" FOREIGN KEY (player_profile_id) REFERENCES player_profiles (id) ON DELETE CASCADE
);

CREATE TABLE player_run_statistics (
    id uuid NOT NULL,
    player_profile_id uuid NOT NULL,
    run_id uuid NOT NULL,
    seed character varying(128) NOT NULL,
    final_depth integer NOT NULL,
    outcome character varying(32) NOT NULL,
    generator_version character varying(64) NOT NULL,
    started_at_utc timestamp with time zone,
    ended_at_utc timestamp with time zone,
    total_damage_dealt integer NOT NULL DEFAULT 0,
    total_damage_taken integer NOT NULL DEFAULT 0,
    total_guard_absorbed integer NOT NULL DEFAULT 0,
    total_healing_done integer NOT NULL DEFAULT 0,
    combats_won integer NOT NULL DEFAULT 0,
    combats_lost integer NOT NULL DEFAULT 0,
    rewards_selected integer NOT NULL DEFAULT 0,
    total_items_used integer NOT NULL DEFAULT 0,
    CONSTRAINT "PK_player_run_statistics" PRIMARY KEY (id),
    CONSTRAINT "FK_player_run_statistics_player_profiles_player_profile_id" FOREIGN KEY (player_profile_id) REFERENCES player_profiles (id) ON DELETE CASCADE
);

CREATE TABLE player_character_items (
    id uuid NOT NULL,
    player_character_id uuid NOT NULL,
    item_definition_key character varying(160) NOT NULL,
    acquired_at_utc timestamp with time zone NOT NULL,
    source character varying(64),
    is_equipped boolean NOT NULL DEFAULT FALSE,
    equipment_slot character varying(16) NOT NULL DEFAULT 'Relic',
    CONSTRAINT "PK_player_character_items" PRIMARY KEY (id),
    CONSTRAINT "FK_player_character_items_player_characters_player_character_id" FOREIGN KEY (player_character_id) REFERENCES player_characters (id) ON DELETE CASCADE
);

CREATE TABLE player_character_skills (
    id uuid NOT NULL,
    player_character_id uuid NOT NULL,
    skill_definition_key character varying(160) NOT NULL,
    unlocked_at_utc timestamp with time zone NOT NULL,
    source character varying(64),
    is_equipped boolean NOT NULL DEFAULT FALSE,
    "EquipmentSlot" text NOT NULL,
    CONSTRAINT "PK_player_character_skills" PRIMARY KEY (id),
    CONSTRAINT "FK_player_character_skills_player_characters_player_character_~" FOREIGN KEY (player_character_id) REFERENCES player_characters (id) ON DELETE CASCADE
);

CREATE TABLE player_character_stat_blocks (
    id uuid NOT NULL,
    player_character_id uuid NOT NULL,
    max_vitality integer NOT NULL DEFAULT 100,
    attack_power integer NOT NULL DEFAULT 12,
    defense integer NOT NULL DEFAULT 6,
    starting_guard integer NOT NULL DEFAULT 0,
    speed integer NOT NULL DEFAULT 10,
    initiative integer NOT NULL DEFAULT 10,
    focus integer NOT NULL DEFAULT 0,
    mana integer NOT NULL DEFAULT 0,
    charge integer NOT NULL DEFAULT 0,
    magic_attack integer NOT NULL DEFAULT 0,
    magic_defense integer NOT NULL DEFAULT 0,
    movement integer NOT NULL DEFAULT 4,
    CONSTRAINT "PK_player_character_stat_blocks" PRIMARY KEY (id),
    CONSTRAINT "FK_player_character_stat_blocks_player_characters_player_chara~" FOREIGN KEY (player_character_id) REFERENCES player_characters (id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_account_identities_account_id" ON account_identities (account_id);

CREATE UNIQUE INDEX "IX_account_identities_email" ON account_identities (email);

CREATE INDEX "IX_account_privacy_consents_account_id_purpose_key_granted_at_~" ON account_privacy_consents (account_id, purpose_key, granted_at_utc);

CREATE INDEX "IX_account_security_tokens_account_id" ON account_security_tokens (account_id);

CREATE UNIQUE INDEX "IX_account_security_tokens_purpose_token_hash" ON account_security_tokens (purpose, token_hash);

CREATE INDEX "IX_account_sessions_account_id" ON account_sessions (account_id);

CREATE UNIQUE INDEX "IX_player_character_items_player_character_id_item_definition_~" ON player_character_items (player_character_id, item_definition_key);

CREATE UNIQUE INDEX "IX_player_character_skills_player_character_id_skill_definitio~" ON player_character_skills (player_character_id, skill_definition_key);

CREATE UNIQUE INDEX "IX_player_character_stat_blocks_player_character_id" ON player_character_stat_blocks (player_character_id);

CREATE INDEX "IX_player_characters_player_profile_id" ON player_characters (player_profile_id);

CREATE INDEX "IX_player_characters_player_profile_id_definition_key" ON player_characters (player_profile_id, definition_key);

CREATE UNIQUE INDEX "IX_player_npc_reputation_scores_player_profile_id_npc_key" ON player_npc_reputation_scores (player_profile_id, npc_key);

CREATE UNIQUE INDEX "IX_player_permanent_items_player_profile_id_item_definition_key" ON player_permanent_items (player_profile_id, item_definition_key);

CREATE UNIQUE INDEX "IX_player_permanent_unlocks_player_profile_id_unlock_key" ON player_permanent_unlocks (player_profile_id, unlock_key);

CREATE INDEX "IX_player_processed_integration_events_processed_at_utc" ON player_processed_integration_events (processed_at_utc);

CREATE INDEX "IX_player_profiles_auth_subject_id" ON player_profiles (auth_subject_id);

CREATE INDEX "IX_player_run_statistics_player_profile_id" ON player_run_statistics (player_profile_id);

CREATE UNIQUE INDEX "IX_player_run_statistics_run_id" ON player_run_statistics (run_id);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260831075028_InitialV1_0_0', '10.0.8');

COMMIT;

