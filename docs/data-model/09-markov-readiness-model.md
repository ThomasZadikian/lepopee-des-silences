# 09 — Markov readiness model

## Purpose

This document defines the data fields required for future Markov-driven adaptive selection. **Markov algorithms are not implemented in this PR.** This document only specifies what the data model must support.

## Why Markov readiness matters

Markov chains and adaptive selection will influence many game systems:

- **Room generation.** Transition probabilities between room types based on current state.
- **Enemy selection.** Choosing enemies based on room type, depth, risk, and recent encounters.
- **Item/reward selection.** Offering rewards that complement the player's current build.
- **Law/Curse appearance.** Timing and probability of law and curse events.
- **Narrative tone.** Adapting story elements based on player behavior patterns.
- **Difficulty adaptation.** Adjusting challenge based on player performance.
- **Boss selection.** Choosing bosses that create appropriate thematic and mechanical contrast.

By including Markov-ready metadata fields in Catalog definitions now, we ensure future adaptive systems can query candidates efficiently without schema changes.

## Fields required on Catalog definitions

These fields must be present on all Catalog entities that participate in adaptive selection:

### Common metadata fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `key` | VARCHAR(160) | YES | Unique definition identifier (already exists) |
| `tags` | relational table | YES | Classification tags (already exists as JSON, needs relational migration) |
| `archetype` | VARCHAR(64) | varies | Entity archetype (already exists on enemies) |
| `family` | VARCHAR(64) | NO | Entity family/group (new) |
| `role` | VARCHAR(64) | NO | Functional role (new for enemies) |
| `base_weight` | INT | YES | Base selection weight (new) |
| `min_depth` | INT | NO | Minimum room depth for appearance (new) |
| `max_depth` | INT | NO | Maximum room depth for appearance (new) |
| `selection_group` | VARCHAR(64) | NO | Logical grouping for selection (new) |
| `compatibility_tags` | relational table | NO | Tags that enable compatibility with other entities (new) |
| `exclusion_tags` | relational table | NO | Tags that prevent co-appearance (new) |

### Per-entity field mapping

| Entity | `key` | `tags` | `archetype` | `family` | `role` | `base_weight` | `min_depth` | `max_depth` | `selection_group` |
|--------|-------|--------|-------------|----------|--------|---------------|-------------|-------------|-------------------|
| `EnemyDefinition` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `SkillDefinition` | ✅ | ✅ | — | — | — | ✅ | — | — | ✅ |
| `ItemDefinition` | ✅ | ✅ | — | — | — | ✅ | ✅ | ✅ | ✅ |
| `PalaceLawDefinition` | ✅ | ✅ | — | — | — | ✅ | ✅ | ✅ | ✅ |
| `CurseDefinition` | ✅ | ✅ | — | — | — | ✅ | ✅ | ✅ | ✅ |
| `RewardTemplate` | ✅ | ✅ | — | — | — | ✅ | ✅ | ✅ | ✅ |
| `RoomBossDefinition` | ✅ | ✅ | — | — | — | ✅ | — | — | ✅ |
| `EventTemplate` | ✅ | ✅ | — | — | — | ✅ | ✅ | ✅ | ✅ |

## How Markov/adaptive selection works

### Input: candidates + context

```text
Catalog provides:
  - List of eligible candidates (filtered by depth, room type, risk level)
  - Each candidate has: key, tags, archetype, family, role, base_weight, compatibility/exclusion tags

Game Engine provides:
  - Current run state (depth, room type, active laws, active curses, inventory)
  - Recent encounter history (last N enemies, last N items)
  - Current difficulty profile (risk level, difficulty multiplier)
  - Player behavior patterns (aggressive, defensive, exploratory)
```

### Selection algorithm (conceptual)

```text
1. Filter candidates by hard constraints:
   - min_depth <= current_depth <= max_depth
   - compatible with current room type
   - not excluded by active tags

2. Compute adaptive weights:
   - base_weight from Catalog
   - × depth_factor (based on current depth vs candidate depth range)
   - × recency_factor (reduce weight if recently encountered)
   - × synergy_factor (increase weight if synergistic with current build)
   - × narrative_factor (increase weight if narratively appropriate)
   - × difficulty_factor (adjust based on player performance)

3. Normalize weights to probabilities

4. Sample using deterministic seeded random (for reproducibility)
```

### Output: snapshot

The selected candidate's `key` is stored in the Game Engine runtime. The selection result is deterministic given the same seed and context.

## Rules

1. **Catalog provides candidates and metadata.** It does not run selection algorithms.
2. **Game Engine provides context and runs selection.** It does not store Catalog metadata permanently.
3. **Selection results are snapshoted.** The chosen `key` is stored, not the probability matrix.
4. **No service depends on Markov internals.** The selection algorithm can be changed without schema changes.
5. **Metadata fields are relational or tagged stably.** No hardcoded conditions in scattered code.
6. **Tags are relational tables, not JSON columns.** This enables efficient filtering and joining.

## Current state and gaps

| Field | Current state | Gap |
|-------|--------------|-----|
| `key` | Exists on all definitions | None |
| `tags` | JSON columns on some entities | Need relational migration |
| `archetype` | Exists on EnemyDefinition (string) | Need on other entities |
| `family` | Does not exist | New field |
| `role` | Does not exist | New field |
| `base_weight` | Does not exist (implicit via `base_difficulty`) | New field |
| `min_depth` | Does not exist on most entities | New field |
| `max_depth` | Does not exist on most entities | New field |
| `selection_group` | Does not exist | New field |
| `compatibility_tags` | Does not exist | New relational table |
| `exclusion_tags` | Does not exist | New relational table |

## Tables affected

| Table | New columns | New tables |
|-------|------------|------------|
| `catalog_enemy_definitions` | `family`, `role`, `base_weight`, `min_depth`, `max_depth`, `selection_group` | `catalog_enemy_tags` (relational) |
| `catalog_skill_definitions` | `base_weight`, `selection_group` | `catalog_skill_tags` (relational) |
| `catalog_item_definitions` | `base_weight`, `min_depth`, `max_depth`, `selection_group` | `catalog_item_tags` (relational) |
| `catalog_palace_law_definitions` | `base_weight`, `min_depth`, `max_depth`, `selection_group` | `catalog_law_tags` (relational) |
| `catalog_curse_definitions` | `base_weight`, `min_depth`, `max_depth`, `selection_group` | `catalog_curse_tags` (relational) |
| `catalog_reward_templates` | `base_weight`, `min_depth`, `max_depth`, `selection_group` | `catalog_reward_tags` (relational) |
| `catalog_room_boss_definitions` | `base_weight`, `selection_group` | `catalog_boss_tags` (relational) |
| `catalog_event_templates` | `base_weight`, `min_depth`, `max_depth`, `selection_group` | `catalog_event_tags` (relational) |

New tables: `catalog_compatibility_tags`, `catalog_exclusion_tags` (junction tables linking definitions).
