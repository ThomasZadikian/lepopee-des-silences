# 04 — Catalog relational schema

## Overview

The Catalog service owns stable, versioned, administrable content definitions. This document defines the target relational schema for all Catalog tables.

## catalog_versions

Tracks seed versions applied to the database.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `seed_key` | VARCHAR(128) | NOT NULL | — | Logical seed identifier |
| `version` | VARCHAR(64) | NOT NULL | — | Version string |
| `checksum` | VARCHAR(256) | NULL | NULL | Optional integrity check |
| `applied_at_utc` | TIMESTAMPTZ | NOT NULL | now() | When seed was applied |

**Primary key:** `id`
**Unique constraints:** `(seed_key, version)`
**Indexes:** none additional
**Relations:** none

---

## catalog_tags

Reusable tag vocabulary for Catalog entities.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `tag_key` | VARCHAR(128) | NOT NULL | — | Canonical tag identifier |
| `display_name` | VARCHAR(256) | NOT NULL | — | Human-readable label |
| `category` | VARCHAR(64) | NULL | NULL | Tag category (archetype, room, element, etc.) |
| `created_at_utc` | TIMESTAMPTZ | NOT NULL | now() | Creation timestamp |

**Primary key:** `id`
**Unique constraints:** `tag_key`
**Indexes:** `category`
**Relations:** Referenced by `*_tags` junction tables

---

## catalog_enemy_definitions

Stable enemy templates used by Game Engine for encounter composition.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `key` | VARCHAR(160) | NOT NULL | — | Unique definition key (e.g., `enemy.threshold.doubt-fragment`) |
| `display_name` | VARCHAR(256) | NOT NULL | — | Display name |
| `description` | TEXT | NOT NULL | — | Lore/flavor text |
| `narrative_text` | TEXT | NULL | NULL | Additional narrative context |
| `archetype` | VARCHAR(64) | NOT NULL | — | Enemy archetype (Fragile, Guard, Bruiser, etc.) |
| `family` | VARCHAR(64) | NULL | NULL | Enemy family (Trauma, Memory, Shadow, etc.) |
| `rank` | VARCHAR(64) | NOT NULL | — | Rank classification (Common, Elite, Boss) |
| `role` | VARCHAR(64) | NULL | NULL | Combat role (Tank, DPS, Support, Disruptor) |
| `base_difficulty` | INT | NOT NULL | — | Difficulty rating (1–20) |
| `encounter_weight` | INT | NOT NULL | 1 | Weight in encounter composition |
| `min_depth` | INT | NOT NULL | 1 | Minimum room depth for appearance |
| `max_depth` | INT | NOT NULL | 10 | Maximum room depth for appearance |
| `is_boss` | BOOLEAN | NOT NULL | FALSE | Whether this is a boss enemy |
| `is_elite` | BOOLEAN | NOT NULL | FALSE | Whether this is an elite enemy |
| `stat_block_id` | UUID | NOT NULL | — | FK → `catalog_enemy_stat_blocks.id` |
| `reward_profile_key` | VARCHAR(128) | NULL | NULL | Reference to reward profile |
| `version` | VARCHAR(32) | NOT NULL | — | Definition version |
| `status` | VARCHAR(32) | NOT NULL | — | Draft / Active / Deprecated / Disabled |
| `created_at_utc` | TIMESTAMPTZ | NOT NULL | now() | Creation timestamp |
| `updated_at_utc` | TIMESTAMPTZ | NOT NULL | now() | Last update timestamp |

**Primary key:** `id`
**Unique constraints:** `key`
**Indexes:** `status`, `archetype`, `rank`, `is_boss`, `(min_depth, max_depth)`
**Relations:** FK → `catalog_enemy_stat_blocks.id`; many-to-many with `catalog_tags` via `catalog_enemy_tags`; many-to-many with `catalog_skill_definitions` via `catalog_enemy_skill_links`

---

## catalog_enemy_stat_blocks

Base combat stats for enemy definitions. Separated to allow stat reuse across enemy variants.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `enemy_definition_id` | UUID | NOT NULL | — | FK → `catalog_enemy_definitions.id` (UNIQUE) |
| `max_vitality` | INT | NOT NULL | — | Base max vitality |
| `attack_power` | INT | NOT NULL | — | Base attack power |
| `defense` | INT | NOT NULL | — | Base defense |
| `starting_guard` | INT | NOT NULL | 0 | Base starting guard |
| `speed` | INT | NOT NULL | — | Base speed |
| `initiative` | INT | NOT NULL | 0 | Base initiative (ATB-ready) |
| `recovery` | INT | NOT NULL | 0 | Base recovery (ATB-ready) |
| `focus` | INT | NOT NULL | 0 | Base focus |

**Primary key:** `id`
**Unique constraints:** `enemy_definition_id`
**Indexes:** none additional
**Relations:** FK → `catalog_enemy_definitions.id` (1:1)

---

## catalog_enemy_skill_links

Junction table linking enemies to their available skills.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `enemy_definition_id` | UUID | NOT NULL | — | FK → `catalog_enemy_definitions.id` |
| `skill_definition_key` | VARCHAR(160) | NOT NULL | — | Reference to `catalog_skill_definitions.key` |

**Primary key:** composite `(enemy_definition_id, skill_definition_key)`
**Indexes:** `skill_definition_key`
**Relations:** FK → `catalog_enemy_definitions.id`

---

## catalog_enemy_tags

Junction table linking enemies to tags.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `enemy_definition_id` | UUID | NOT NULL | — | FK → `catalog_enemy_definitions.id` |
| `tag_id` | UUID | NOT NULL | — | FK → `catalog_tags.id` |

**Primary key:** composite `(enemy_definition_id, tag_id)`
**Indexes:** `tag_id`
**Relations:** FK → both parent tables

---

## catalog_skill_definitions

Stable skill templates used by enemies and players.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `key` | VARCHAR(160) | NOT NULL | — | Unique definition key (e.g., `skill.basic.strike`) |
| `display_name` | VARCHAR(256) | NOT NULL | — | Display name |
| `description` | TEXT | NOT NULL | — | Description |
| `narrative_text` | TEXT | NULL | NULL | Additional narrative context |
| `skill_type` | VARCHAR(64) | NOT NULL | — | Skill type (Damage, Defense, Debuff, Buff, etc.) |
| `targeting_mode` | VARCHAR(64) | NOT NULL | — | Targeting mode (Self, SingleEnemy, AllEnemies, etc.) |
| `effect_type` | VARCHAR(64) | NOT NULL | — | Primary effect type |
| `cost_type` | VARCHAR(32) | NOT NULL | — | Cost resource type (Mana, Charge, Focus, None) |
| `cost_amount` | INT | NOT NULL | 0 | Cost amount |
| `power` | INT | NOT NULL | 0 | Base power (damage or heal amount) |
| `accuracy` | INT | NOT NULL | 100 | Hit chance (0–100) |
| `action_cost` | INT | NOT NULL | 10 | ATB action cost (ticks consumed) |
| `cast_time` | INT | NOT NULL | 0 | ATB cast time (ticks before resolution) |
| `recovery_time` | INT | NOT NULL | 0 | ATB recovery time (ticks added after use) |
| `cooldown` | INT | NOT NULL | 0 | Turns before reuse (0 = no cooldown) |
| `effect_set_id` | UUID | NULL | NULL | FK → `catalog_effect_sets.id` |
| `version` | VARCHAR(32) | NOT NULL | — | Definition version |
| `status` | VARCHAR(32) | NOT NULL | — | Draft / Active / Deprecated / Disabled |
| `created_at_utc` | TIMESTAMPTZ | NOT NULL | now() | Creation timestamp |
| `updated_at_utc` | TIMESTAMPTZ | NOT NULL | now() | Last update timestamp |

**Primary key:** `id`
**Unique constraints:** `key`
**Indexes:** `status`, `skill_type`, `targeting_mode`
**Relations:** FK → `catalog_effect_sets.id` (optional)

---

## catalog_skill_effects

Explicit effect entries for skills (replaces implicit effect logic).

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `skill_definition_id` | UUID | NOT NULL | — | FK → `catalog_skill_definitions.id` |
| `effect_type` | VARCHAR(64) | NOT NULL | — | EffectType enum value |
| `target_scope` | VARCHAR(64) | NOT NULL | — | EffectTargetScope enum value |
| `value` | INT | NOT NULL | — | Effect value |
| `value_mode` | VARCHAR(32) | NOT NULL | 'Flat' | ValueMode enum value |
| `duration` | VARCHAR(64) | NOT NULL | 'Immediate' | EffectDuration enum value |
| `stack_policy` | VARCHAR(32) | NOT NULL | 'None' | StackPolicy enum value |
| `condition` | VARCHAR(256) | NULL | NULL | Optional condition expression |

**Primary key:** `id`
**Indexes:** `skill_definition_id`
**Relations:** FK → `catalog_skill_definitions.id` (1:N)

---

## catalog_item_definitions

Stable item templates used in rewards, merchants, and inventory.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `key` | VARCHAR(160) | NOT NULL | — | Unique definition key (e.g., `item.consumable.baume-de-memoire`) |
| `display_name` | VARCHAR(256) | NOT NULL | — | Display name |
| `description` | TEXT | NOT NULL | — | Description |
| `narrative_text` | TEXT | NULL | NULL | Additional narrative context |
| `category` | VARCHAR(64) | NOT NULL | — | Item category (Consumable, Relic, Fragment, etc.) |
| `item_type` | VARCHAR(64) | NOT NULL | — | Item type (Heal, Guard, ResourceRestore, etc.) |
| `rarity` | VARCHAR(64) | NOT NULL | — | Rarity (Common, Uncommon, Rare, Epic, Legendary, Unique) |
| `usage_mode` | VARCHAR(64) | NOT NULL | — | Usage mode (Passive, UseInCombat, UseOnNode, NotUsable) |
| `lifecycle` | VARCHAR(64) | NOT NULL | — | Lifecycle (RuntimeRunOnly, RuntimeRoomOnly, PermanentUnlockCandidate) |
| `stack_policy` | VARCHAR(32) | NOT NULL | 'Additive' | How quantities stack |
| `max_stack` | INT | NOT NULL | 1 | Maximum stack size |
| `is_usable_in_combat` | BOOLEAN | NOT NULL | FALSE | Whether usable during combat |
| `is_usable_outside_combat` | BOOLEAN | NOT NULL | FALSE | Whether usable outside combat |
| `effect_set_id` | UUID | NULL | NULL | FK → `catalog_effect_sets.id` |
| `min_depth` | INT | NULL | NULL | Minimum room depth for appearance |
| `max_depth` | INT | NULL | NULL | Maximum room depth for appearance |
| `base_weight` | INT | NOT NULL | 1 | Weight in reward/merchant selection |
| `version` | VARCHAR(32) | NOT NULL | — | Definition version |
| `status` | VARCHAR(32) | NOT NULL | — | Draft / Active / Deprecated / Disabled |
| `created_at_utc` | TIMESTAMPTZ | NOT NULL | now() | Creation timestamp |
| `updated_at_utc` | TIMESTAMPTZ | NOT NULL | now() | Last update timestamp |

**Primary key:** `id`
**Unique constraints:** `key`
**Indexes:** `status`, `category`, `rarity`, `item_type`, `(min_depth, max_depth)`
**Relations:** FK → `catalog_effect_sets.id` (optional); many-to-many with `catalog_tags` via `catalog_item_tags`

---

## catalog_item_effects

Explicit effect entries for items.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `item_definition_id` | UUID | NOT NULL | — | FK → `catalog_item_definitions.id` |
| `effect_type` | VARCHAR(64) | NOT NULL | — | EffectType enum value |
| `target_scope` | VARCHAR(64) | NOT NULL | — | EffectTargetScope enum value |
| `value` | INT | NOT NULL | — | Effect value |
| `value_mode` | VARCHAR(32) | NOT NULL | 'Flat' | ValueMode enum value |
| `duration` | VARCHAR(64) | NOT NULL | 'Immediate' | EffectDuration enum value |
| `stack_policy` | VARCHAR(32) | NOT NULL | 'None' | StackPolicy enum value |

**Primary key:** `id`
**Indexes:** `item_definition_id`
**Relations:** FK → `catalog_item_definitions.id` (1:N)

---

## catalog_item_tags

Junction table linking items to tags.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `item_definition_id` | UUID | NOT NULL | — | FK → `catalog_item_definitions.id` |
| `tag_id` | UUID | NOT NULL | — | FK → `catalog_tags.id` |

**Primary key:** composite `(item_definition_id, tag_id)`
**Relations:** FK → both parent tables

---

## catalog_palace_law_definitions

Stable palace law templates.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `key` | VARCHAR(160) | NOT NULL | — | Unique definition key (e.g., `law.threshold.silence-weight`) |
| `display_name` | VARCHAR(256) | NOT NULL | — | Display name |
| `description` | TEXT | NOT NULL | — | Description |
| `narrative_text` | TEXT | NULL | NULL | Additional narrative context |
| `scope` | VARCHAR(64) | NOT NULL | — | Scope (Room, Run, Permanent) |
| `duration` | VARCHAR(64) | NOT NULL | — | Default duration |
| `trigger` | VARCHAR(64) | NULL | NULL | Activation trigger condition |
| `severity` | INT | NOT NULL | 1 | Severity level (1–5) |
| `effect_set_id` | UUID | NULL | NULL | FK → `catalog_effect_sets.id` |
| `base_weight` | INT | NOT NULL | 1 | Weight in law selection |
| `min_depth` | INT | NULL | NULL | Minimum room depth |
| `max_depth` | INT | NULL | NULL | Maximum room depth |
| `visibility` | VARCHAR(32) | NOT NULL | — | Visibility (Visible, PartiallyVisible, Hidden) |
| `priority` | INT | NOT NULL | 0 | Selection priority |
| `version` | VARCHAR(32) | NOT NULL | — | Definition version |
| `status` | VARCHAR(32) | NOT NULL | — | Draft / Active / Deprecated / Disabled |
| `created_at_utc` | TIMESTAMPTZ | NOT NULL | now() | Creation timestamp |
| `updated_at_utc` | TIMESTAMPTZ | NOT NULL | now() | Last update timestamp |

**Primary key:** `id`
**Unique constraints:** `key`
**Indexes:** `status`, `visibility`, `(min_depth, max_depth)`
**Relations:** FK → `catalog_effect_sets.id` (optional)

---

## catalog_palace_law_effects

Explicit effect entries for palace laws.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `palace_law_definition_id` | UUID | NOT NULL | — | FK → `catalog_palace_law_definitions.id` |
| `effect_type` | VARCHAR(64) | NOT NULL | — | EffectType enum value |
| `target_scope` | VARCHAR(64) | NOT NULL | — | EffectTargetScope enum value |
| `value` | DECIMAL(10,4) | NOT NULL | — | Effect value (can be fractional for multipliers) |
| `value_mode` | VARCHAR(32) | NOT NULL | 'Flat' | ValueMode enum value |
| `duration` | VARCHAR(64) | NOT NULL | — | EffectDuration enum value |
| `stack_policy` | VARCHAR(32) | NOT NULL | 'Additive' | StackPolicy enum value |

**Primary key:** `id`
**Indexes:** `palace_law_definition_id`
**Relations:** FK → `catalog_palace_law_definitions.id` (1:N)

---

## catalog_curse_definitions

Stable curse templates. Currently does not exist in the codebase.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `key` | VARCHAR(160) | NOT NULL | — | Unique definition key (e.g., `curse.threshold.souffle-lourd`) |
| `display_name` | VARCHAR(256) | NOT NULL | — | Display name |
| `description` | TEXT | NOT NULL | — | Description |
| `narrative_text` | TEXT | NULL | NULL | Additional narrative context |
| `severity` | INT | NOT NULL | 1 | Severity level (1–5) |
| `duration` | VARCHAR(64) | NOT NULL | — | Default duration (NextCombatOnly, UntilRoomEnds, etc.) |
| `trigger` | VARCHAR(64) | NULL | NULL | Activation trigger |
| `effect_set_id` | UUID | NULL | NULL | FK → `catalog_effect_sets.id` |
| `base_weight` | INT | NOT NULL | 1 | Weight in curse selection |
| `min_depth` | INT | NULL | NULL | Minimum room depth |
| `max_depth` | INT | NULL | NULL | Maximum room depth |
| `version` | VARCHAR(32) | NOT NULL | — | Definition version |
| `status` | VARCHAR(32) | NOT NULL | — | Draft / Active / Deprecated / Disabled |
| `created_at_utc` | TIMESTAMPTZ | NOT NULL | now() | Creation timestamp |
| `updated_at_utc` | TIMESTAMPTZ | NOT NULL | now() | Last update timestamp |

**Primary key:** `id`
**Unique constraints:** `key`
**Indexes:** `status`, `severity`, `(min_depth, max_depth)`
**Relations:** FK → `catalog_effect_sets.id` (optional)

---

## catalog_curse_effects

Explicit effect entries for curses.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `curse_definition_id` | UUID | NOT NULL | — | FK → `catalog_curse_definitions.id` |
| `effect_type` | VARCHAR(64) | NOT NULL | — | EffectType enum value |
| `target_scope` | VARCHAR(64) | NOT NULL | — | EffectTargetScope enum value |
| `value` | DECIMAL(10,4) | NOT NULL | — | Effect value |
| `value_mode` | VARCHAR(32) | NOT NULL | 'Flat' | ValueMode enum value |
| `duration` | VARCHAR(64) | NOT NULL | — | EffectDuration enum value |
| `stack_policy` | VARCHAR(32) | NOT NULL | 'None' | StackPolicy enum value |

**Primary key:** `id`
**Indexes:** `curse_definition_id`
**Relations:** FK → `catalog_curse_definitions.id` (1:N)

---

## catalog_reward_templates

Stable reward templates used to generate reward offers.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `key` | VARCHAR(160) | NOT NULL | — | Unique template key |
| `display_name` | VARCHAR(256) | NOT NULL | — | Display name |
| `description` | TEXT | NOT NULL | — | Description |
| `source_type` | VARCHAR(64) | NOT NULL | — | Reward source (Combat, Elite, RoomBoss, Item, Rest, Npc, Merchant, Law) |
| `min_choices` | INT | NOT NULL | 1 | Minimum number of choices offered |
| `max_choices` | INT | NOT NULL | 3 | Maximum number of choices offered |
| `min_depth` | INT | NULL | NULL | Minimum room depth |
| `max_depth` | INT | NULL | NULL | Maximum room depth |
| `base_weight` | INT | NOT NULL | 1 | Weight in reward selection |
| `version` | VARCHAR(32) | NOT NULL | — | Template version |
| `status` | VARCHAR(32) | NOT NULL | — | Draft / Active / Deprecated / Disabled |
| `created_at_utc` | TIMESTAMPTZ | NOT NULL | now() | Creation timestamp |
| `updated_at_utc` | TIMESTAMPTZ | NOT NULL | now() | Last update timestamp |

**Primary key:** `id`
**Unique constraints:** `key`
**Indexes:** `status`, `source_type`
**Relations:** 1:N → `catalog_reward_template_options`

---

## catalog_reward_template_options

Individual reward choices within a reward template.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `reward_template_id` | UUID | NOT NULL | — | FK → `catalog_reward_templates.id` |
| `reward_type` | VARCHAR(64) | NOT NULL | — | Reward type (Heal, TemporaryItem, StatBonus, MemoryFragment) |
| `label` | VARCHAR(256) | NOT NULL | — | Display label |
| `description` | TEXT | NOT NULL | — | Description |
| `payload_key` | VARCHAR(256) | NULL | NULL | Reference to item/fragment/stat key |
| `base_amount` | INT | NOT NULL | — | Base amount before scaling |
| `scaling_mode` | VARCHAR(32) | NOT NULL | 'Flat' | How amount scales with difficulty |
| `weight` | INT | NOT NULL | 1 | Selection weight within template |

**Primary key:** `id`
**Indexes:** `reward_template_id`
**Relations:** FK → `catalog_reward_templates.id` (N:1)

---

## catalog_effect_sets

Reusable effect set containers.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `key` | VARCHAR(160) | NOT NULL | — | Unique set key |
| `display_name` | VARCHAR(256) | NOT NULL | — | Display name |
| `description` | TEXT | NULL | NULL | Description |
| `version` | VARCHAR(32) | NOT NULL | — | Version |
| `created_at_utc` | TIMESTAMPTZ | NOT NULL | now() | Creation timestamp |

**Primary key:** `id`
**Unique constraints:** `key`
**Relations:** 1:N → `catalog_effect_definitions`

---

## catalog_effect_definitions

Individual effects within an effect set.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | UUID | NOT NULL | gen_random_uuid() | Primary key |
| `effect_set_id` | UUID | NOT NULL | — | FK → `catalog_effect_sets.id` |
| `effect_type` | VARCHAR(64) | NOT NULL | — | EffectType enum value |
| `target_scope` | VARCHAR(64) | NOT NULL | — | EffectTargetScope enum value |
| `value` | DECIMAL(10,4) | NOT NULL | — | Effect value |
| `value_mode` | VARCHAR(32) | NOT NULL | 'Flat' | ValueMode enum value |
| `duration` | VARCHAR(64) | NOT NULL | 'Immediate' | EffectDuration enum value |
| `stack_policy` | VARCHAR(32) | NOT NULL | 'None' | StackPolicy enum value |
| `condition` | VARCHAR(256) | NULL | NULL | Optional condition expression |
| `order` | INT | NOT NULL | 0 | Execution order within set |

**Primary key:** `id`
**Indexes:** `effect_set_id`, `order`
**Relations:** FK → `catalog_effect_sets.id` (N:1)

---

## Enums (Catalog)

| Enum | Values |
|------|--------|
| `ItemCategory` | Fragment, Consumable, Relic, Memory, Catalyst, DefensiveCharm, OffensiveCharm |
| `ItemType` | Heal, Guard, ResourceRestore, CombatDebuff, RunPassive, RoomPassive, NarrativeFragment |
| `UsageMode` | Passive, UseInCombat, UseOnNode, UseOnReward, NotUsable |
| `Lifecycle` | RuntimeRunOnly, RuntimeRoomOnly, PermanentUnlockCandidate, CatalogOnly |
| `EnemyArchetype` | Fragile, Guard, Bruiser, Skirmisher, Disruptor, Support, Elite, Boss |
| `EnemyFamily` | Trauma, Memory, Shadow, Guardian, Silence, Rupture, Forest |
| `EnemyRank` | Common, Elite, Boss |
| `EnemyRole` | Tank, DPS, Support, Disruptor |
| `SkillType` | Damage, Defense, Debuff, Buff, Utility |
| `TargetingMode` | Self, SingleAlly, AllAllies, SingleEnemy, AllEnemies, AnySingle |
| `EffectType` | HealVitality, DamageVitality, AddCurrentGuard, AddStartingGuard, ModifyAttackPower, ModifyDefense, ModifySpeed, ModifyInitiative, ModifyRecovery, ModifyDifficultyMultiplier, ModifyRewardPowerMultiplier, RestoreFocus, ApplyWeaken, ApplyDisrupt, GrantRunItem, GrantRunModifier, GrantPermanentUnlockCandidate |
| `EffectTargetScope` | Self, SingleEnemy, AllEnemies, SingleAlly, AllAllies, Run, Room, NextCombat, NextReward |
| `EffectDuration` | Immediate, CurrentCombat, NextCombatOnly, NextRewardOnly, UntilRoomEnds, UntilRunEnds, PermanentCandidate |
| `StackPolicy` | None, Additive, HighestOnly, RefreshDuration, Replace |
| `ValueMode` | Flat, Percent, Multiplier |
| `PalaceLawVisibility` | Visible, PartiallyVisible, Hidden |
| `PalaceLawScope` | Room, Run, Permanent |
| `RewardType` | Heal, TemporaryItem, StatBonus, MemoryFragment |
| `RewardSource` | Combat, Elite, RoomBoss, Item, Rest, Npc, Merchant, Law |
