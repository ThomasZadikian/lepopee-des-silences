# 10 — Migration roadmap from current state

## Overview

This document defines the step-by-step migration from the current codebase to the `data-model-0.1` target schema. Each step is designed to be non-breaking and backward-compatible.

## Current state summary

### Catalog

| Entity | Current EF table | Current state |
|--------|-----------------|---------------|
| `EnemyDefinition` | `catalog_enemy_definitions` | EF-persisted. Uses `CompatibleRoomTypesJson`, `TagsJson`, `SkillKeysJson` (JSON columns). Missing: `family`, `role`, `rank`, `encounter_weight`, `min_depth`, `max_depth`, `is_boss`, `is_elite`, `stat_block_id`, `narrative_text`. |
| `SkillDefinition` | `catalog_skill_definitions` | EF-persisted. Missing: `cost_type`, `accuracy`, `action_cost`, `cast_time`, `recovery_time`, `cooldown`, `effect_set_id`, `narrative_text`. |
| `ItemDefinition` | `catalog_item_definitions` | EF-persisted. Missing: `item_type`, `usage_mode`, `lifecycle`, `stack_policy`, `max_stack`, `is_usable_in_combat`, `is_usable_outside_combat`, `effect_set_id`, `min_depth`, `max_depth`, `base_weight`, `narrative_text`. |
| `PalaceLawDefinition` | `catalog_palace_law_definitions` | EF-persisted. Uses `ImpactDomainsJson` (JSON). Missing: `scope`, `duration`, `trigger`, `severity`, `effect_set_id`, `base_weight`, `min_depth`, `max_depth`, `narrative_text`. |
| `EnemyTemplate` | None (InMemory) | In-memory only. Not a migration target. |
| `SkillTemplate` | None (InMemory) | In-memory only. Not a migration target. |
| `ItemTemplate` | None (InMemory) | In-memory only. Not a migration target. |
| `EventTemplate` | None (InMemory) | In-memory only. Needs EF persistence. |
| `RoomBossDefinition` | None (InMemory) | In-memory only. Needs EF persistence. |
| `CurseDefinition` | None | Does not exist. Needs creation. |
| `RewardTemplate` | None | Does not exist. Needs creation. |
| `EffectSet` | None | Does not exist. Needs creation. |
| `EffectDefinition` | None | Does not exist. Needs creation. |

### Player

| Entity | Current EF table | Current state |
|--------|-----------------|---------------|
| `PlayerProfile` | `player_profiles` | EF-persisted. Progression stats embedded as columns. |
| `PlayerCharacter` | `player_characters` | EF-persisted. Missing: `character_type`, `status`, `updated_at_utc`. Stats (`max_vitality`, `base_mana`, `base_charge`) embedded. Skills as JSON. |
| `PlayerCharacterStatBlock` | None | Does not exist. Need to extract from `player_characters`. |
| `PlayerCharacterSkill` | None | Skills in `skill_keys_json`. Need relational table. |
| `PermanentUnlock` | None | Does not exist. Future feature. |
| `RunStatistics` | None | Does not exist. Future feature. |

### Game Engine

| Entity | Current EF table | Current state |
|--------|-----------------|---------------|
| `Run` | `runs` | EF-persisted. Stats embedded (`max_hp`, `current_hp`, `attack`, `defense`, `speed`). Missing snapshot tables. |
| `Room` | `run_rooms` | EF-persisted. |
| `MapNode` | `run_nodes` | EF-persisted. |
| `Combat` | `run_active_combats` | EF-persisted. |
| `Combatant` | `run_combatants` | EF-persisted. Missing: `attack_power` (uses generic stats). |
| `CombatantSkill` | `run_combatant_skills` | EF-persisted. |
| `PlayerRuntimeState` | `run_player_states` | EF-persisted. Missing: `attack_power`, `defense`, `starting_guard`, `speed`, `initiative`, `recovery`, `focus`. |
| `RunItem` | `run_items` | EF-persisted. |
| `RunModifier` | `run_modifiers` | EF-persisted. Missing: `value_mode`, `stack_policy`, `expires_at_room_id`, `expires_at_combat_id`. |
| `ActivePalaceLaw` | `run_active_palace_laws` | EF-persisted. Missing: `duration`, `expires_at_room_id`, `consumed_at_utc`. |
| `ActiveCurse` | None (domain only) | Exists as domain entity on Run, not persisted as separate table. |
| `RewardOffer` | None (InMemory) | In-memory only. Needs EF persistence. |
| `RunCharacterSnapshot` | None | Does not exist. Needs creation. |
| `RunCombatantStatSnapshot` | None | Does not exist. Needs creation. |
| `RunCombatantEffect` | None | Does not exist. Needs creation. |
| `RunCombatAction` | None | Does not exist. Needs creation. |

## Migration phases

### Phase 1: data-model-0.1 (this PR)

**Scope:** Documentation only. No code changes.

**Deliverables:**
- ADR-006
- All `docs/data-model/*` documents
- README link update

**Risk:** None (documentation only).

---

### Phase 2: alpha-0.7.1 — Catalog schema alignment

**Scope:** Align Catalog EF schema with target relational model.

**Changes:**
1. Add columns to `catalog_enemy_definitions`: `family`, `role`, `rank`, `encounter_weight`, `min_depth`, `max_depth`, `is_boss`, `is_elite`, `narrative_text`.
2. Create `catalog_enemy_stat_blocks` table with all combat stats.
3. Create `catalog_enemy_skill_links` relational table (replaces `skill_keys_json`).
4. Create `catalog_enemy_tags` relational table (replaces `tags_json`).
5. Add columns to `catalog_skill_definitions`: `cost_type`, `accuracy`, `action_cost`, `cast_time`, `recovery_time`, `cooldown`, `narrative_text`.
6. Create `catalog_skill_effects` table.
7. Add columns to `catalog_item_definitions`: `item_type`, `usage_mode`, `lifecycle`, `stack_policy`, `max_stack`, `is_usable_in_combat`, `is_usable_outside_combat`, `min_depth`, `max_depth`, `base_weight`, `narrative_text`.
8. Create `catalog_item_effects` and `catalog_item_tags` tables.
9. Add columns to `catalog_palace_law_definitions`: `scope`, `duration`, `trigger`, `severity`, `base_weight`, `min_depth`, `max_depth`, `narrative_text`. Replace `ImpactDomainsJson` with relational table.
10. Create `catalog_palace_law_effects` table.
11. Create `catalog_curse_definitions` and `catalog_curse_effects` tables.
12. Create `catalog_effect_sets` and `catalog_effect_definitions` tables.
13. Create `catalog_reward_templates` and `catalog_reward_template_options` tables.
14. Create `catalog_tags` table.
15. Update `CatalogSeedRunner` with new seed data.
16. Update InMemory stores to match new schema.

**Backward compatibility:** Additive only. New columns have defaults. Old columns preserved until data migration.

**Risk:** Low. New tables and columns do not affect existing gameplay.

---

### Phase 3: alpha-0.7.2 — Player schema alignment

**Scope:** Align Player EF schema with target relational model.

**Changes:**
1. Create `player_character_stat_blocks` table with all permanent stats.
2. Create `player_character_skills` relational table (replaces `skill_keys_json`).
3. Add columns to `player_characters`: `character_type`, `status`, `updated_at_utc`.
4. Create `player_permanent_unlocks` table (empty, ready for future use).
5. Create `player_run_statistics` table (empty, ready for future use).
6. Migrate data from `player_characters.max_vitality`/`base_mana`/`base_charge` to `player_character_stat_blocks`.
7. Migrate data from `skill_keys_json` to `player_character_skills`.
8. Update `EfPlayerProfileRepository` to use new tables.
9. Update domain entities to match new schema.

**Backward compatibility:** New tables created alongside old. Data migrated. Old columns deprecated but preserved.

**Risk:** Medium. Requires data migration. Test with existing player data.

---

### Phase 4: alpha-0.7.3 — Game Engine runtime stat snapshots

**Scope:** Add runtime stat snapshot tables to Game Engine.

**Changes:**
1. Create `run_player_snapshots` table.
2. Create `run_character_snapshots` table.
3. Create `run_character_stat_snapshots` table with all combat stats.
4. Create `run_character_skill_snapshots` table.
5. Create `run_combatant_stat_snapshots` table.
6. Create `run_combatant_effects` table.
7. Create `run_combat_actions` table.
8. Create `run_active_curses` table.
9. Create `run_reward_offers` and `run_reward_options` tables.
10. Add columns to `run_modifiers`: `value_mode`, `stack_policy`, `expires_at_room_id`, `expires_at_combat_id`.
11. Add columns to `run_active_palace_laws`: `duration`, `expires_at_room_id`, `consumed_at_utc`.
12. Update `EfRunRepository` to persist new tables.
13. Update `DeterministicRunGenerator` to create snapshots at run start.
14. Update combat creation to create stat snapshots.

**Backward compatibility:** Additive only. New tables do not affect existing gameplay flow.

**Risk:** Medium. Requires careful integration with existing run/combat lifecycle.

---

### Phase 5: alpha-0.7.4 — Items/effects/modifiers migration

**Scope:** Migrate item and effect application to the official model.

**Changes:**
1. Update `RunItem` to use new `EffectType` enum values.
2. Update `RunModifier` to use new `StackPolicy` and `ValueMode` fields.
3. Update item use handlers to read effects from `catalog_item_effects` or snapshot.
4. Update law/curse application to create proper `RunModifier` entries with all fields.
5. Update `RewardOfferFactory` to use `catalog_reward_templates`.
6. Migrate hardcoded effect logic to use `EffectDefinition` + `EffectSet`.

**Backward compatibility:** RunModifiers gain new fields with defaults. Existing modifiers continue to work.

**Risk:** Medium. Requires updating multiple application handlers.

---

### Phase 6: alpha-0.8.x — ATB preparation

**Scope:** Add ATB fields and implement ATB alongside round-robin.

**Changes:**
1. Add `initiative`, `recovery` to `catalog_enemy_stat_blocks` and `player_character_stat_blocks` (default 0).
2. Add `action_cost`, `cast_time`, `recovery_time` to `catalog_skill_definitions` (default values).
3. Add `atb_gauge_value`, `atb_ready_threshold`, `action_recovery_until_tick` to `run_combatant_stat_snapshots` (nullable).
4. Add `created_at_tick`, `expires_at_tick` to `run_combatant_effects` (nullable).
5. Implement ATB tick loop alongside round-robin (feature flag).
6. Update `SubmitCombatAction` to use ATB timing.

**Backward compatibility:** All new columns are nullable or have defaults. Round-robin continues to work.

**Risk:** High. Core combat system change. Requires extensive testing.

---

### Phase 7: alpha-0.9.x — Markov readiness

**Scope:** Add Markov-ready metadata fields.

**Changes:**
1. Add `family`, `role`, `base_weight`, `min_depth`, `max_depth`, `selection_group` to Catalog tables.
2. Create relational tag tables for all entities.
3. Create `catalog_compatibility_tags` and `catalog_exclusion_tags` tables.
4. Update seed data with Markov metadata.
5. Implement Markov-based selection in Game Engine (optional, feature flag).

**Backward compatibility:** Additive only.

**Risk:** Low. New fields with defaults.

---

## What can be migrated without breaking gameplay

| Change | Breaking? | Notes |
|--------|-----------|-------|
| Adding new columns with defaults | No | EF additive migration |
| Adding new tables | No | New tables not queried by existing code |
| Adding nullable columns | No | Existing code ignores null |
| Renaming columns | Yes | Requires data migration + code update |
| Changing column types | Yes | Requires data migration |
| Removing columns | Yes | Requires code update first |
| Adding new enum values | No | Existing code handles unknown values |

## Risks and mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| JSON → relational migration loses data | High | Write migration script, verify data integrity |
| New columns conflict with existing queries | Medium | Use additive migrations, test thoroughly |
| Snapshot creation adds latency to run start | Low | Snapshots are small, creation is fast |
| ATB breaks existing combat flow | High | Feature flag, keep round-robin as fallback |
| Markov selection produces poor encounters | Medium | Keep base_weight as fallback, tune over time |
