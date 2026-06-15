# ADR-006 — Gameplay entity ownership and relational schema

## Status

Accepted

## Date

2026-06-15

## Context

L'épopée des silences has reached a functional alpha state with a playable run loop, combat, skills, items, rewards, laws, curses, and a player service. However, the data model has grown organically across three microservices without a formal relational schema specification.

Current problems:

- **No formal stat taxonomy.** Combat stats (`max_vitality`, `attack_power`, `defense`, `speed`, `guard`) exist as ad-hoc integer fields scattered across domain entities, EF entities, and DTOs. There is no single reference defining which stats exist, their types, their ranges, or which service owns them.

- **Inconsistent naming.** The same semantic stat may be called `Attack` in one place, `Strength` in another, and `Power` in a third. The `Combatant` entity uses `Attack`/`Defense` while the legacy `EnemyTemplate` uses `Strength`/`Intelligence`.

- **String-typed enums in Definitions.** Catalog Definitions (`EnemyDefinition`, `SkillDefinition`) store archetype, skill type, targeting type, and effect type as raw strings, while Templates use strongly-typed enums. This creates ambiguity about the canonical enum values.

- **No effect/modifier model.** Effects from items, laws, curses, and skills are applied through scattered code paths with no unified `EffectDefinition` schema. `RunModifier` exists but its type enum is limited and its relationship to effects is implicit.

- **JSON for primary data.** Some primary collections (skill keys, tags, compatible room types) are stored as JSON text columns rather than relational tables, which limits querying and indexing.

- **No ATB readiness.** The current combat system uses simple round-robin turn order. Future ATB (Active Time Battle) requires `initiative`, `recovery`, `action_cost`, `cast_time`, and `atb_gauge_value` fields that do not yet exist in the schema.

- **No Markov readiness.** Future adaptive selection (Markov-driven enemy/item/law selection) requires metadata fields (`tags`, `archetype`, `family`, `role`, `base_weight`, `selection_group`, `compatibility_tags`) that are partially present but not formally defined.

- **Snapshot ambiguity.** It is unclear which fields are snapshotted from Catalog into Game Engine at combat creation, and which fields are live-read. The current code snapshots some fields but reads others from Catalog mid-combat.

## Decision

The project adopts an explicit relational model for all gameplay entities, with formal service ownership, lifecycle rules, and stat taxonomy.

### Core principles

1. **Catalog owns base definitions.** Stable, versioned, administrable content templates and definitions live in the Catalog service. Catalog never stores runtime state.

2. **Player owns permanent player progression.** Character stats, unlocked skills, permanent inventory items, and run statistics live in the Player service. Player never stores combat runtime state.

3. **Game Engine owns runtime snapshots and combat state.** During a run, the Game Engine snapshots the values it needs from Catalog and Player into its own tables. It never reads Catalog or Player databases directly during combat resolution.

4. **Primary stats are explicit relational columns.** `max_vitality`, `current_vitality`, `attack_power`, `defense`, `starting_guard`, `current_guard`, `speed`, `initiative`, `recovery`, and `focus` are integer columns on their respective tables. They are never stored as JSON blobs.

5. **Secondary metadata may use JSON.** Collections that are purely informational and never queried relationally (narrative tags, flavor text arrays) may use JSON columns. Collections that participate in joins, filtering, or indexing must be relational tables.

6. **Snapshots freeze values at creation time.** When a combat starts, enemy stats are computed from `EnemyDefinition` + `DifficultyMultiplier` and stored as `run_combatant_stat_snapshots` rows. Catalog changes after combat creation do not affect the active combat.

7. **Inter-service communication uses keys, not foreign keys.** Services reference each other by `definition_key`, `player_id`, `character_id`, `run_id`, snapshots, and integration events. No cross-database foreign keys exist.

### Combat stats are shared concepts, but their values are service-owned depending on lifecycle

| Stat | Catalog (definition) | Player (permanent) | Game Engine (runtime) |
|------|---------------------|--------------------|-----------------------|
| `max_vitality` | base value on `EnemyDefinition` / `SkillDefinition` | permanent value on `PlayerCharacter` | snapshotted + mutable (`current_vitality`) |
| `attack_power` | base value on `EnemyDefinition` | permanent value on `PlayerCharacter` | snapshotted, modified by RunModifiers |
| `defense` | base value on `EnemyDefinition` | permanent value on `PlayerCharacter` | snapshotted, modified by RunModifiers |
| `starting_guard` | base value on `EnemyDefinition` | permanent value on `PlayerCharacter` | snapshotted, additive from RunModifiers |
| `current_guard` | not applicable | not applicable | runtime-only, mutable during combat |
| `speed` | base value on `EnemyDefinition` | permanent value on `PlayerCharacter` | snapshotted, ATB-ready |
| `initiative` | base value on `EnemyDefinition` | permanent value on `PlayerCharacter` | snapshotted, ATB-ready |
| `recovery` | base value on `EnemyDefinition` | permanent value on `PlayerCharacter` | snapshotted, ATB-ready |
| `focus` | base value on `EnemyDefinition` | permanent value on `PlayerCharacter` | snapshotted, mutable |
| `current_vitality` | not applicable | not applicable | runtime-only, mutable during combat |

## Consequences

### Positive

- A single reference document defines all gameplay entities, their stats, their ownership, and their lifecycle.
- Agents and contributors can no longer invent arbitrary fields; the data model acts as a contract.
- ATB and Markov readiness are built into the schema from the start, avoiding costly retrofits.
- The snapshot model is explicitly documented, eliminating ambiguity about when Catalog values are read.
- Relational columns for primary stats enable proper indexing, querying, and future reporting.

### Negative

- The schema is larger than the current implementation, requiring incremental migration.
- Some current JSON columns (skill_keys_json, tags_json) will need to be migrated to relational tables.
- The Catalog service will need new tables (stat blocks, effect sets, curse definitions) that do not yet exist.

## Alternatives considered

### 1. Store everything in Catalog

Rejected. Catalog would become a monolith owning runtime state, permanent progression, and definitions. This violates the microservice boundary principle and makes Catalog a single point of failure.

### 2. Store everything in Game Engine

Rejected. Game Engine is ephemeral per-run. Permanent player progression and stable content definitions must outlive any single run.

### 3. Store primary stats as JSON blobs

Rejected. JSON blobs cannot be indexed, queried, or joined efficiently. They hide the schema from the database layer and make migrations fragile. Primary stats must be explicit columns.

### 4. Single shared database

Rejected. Cross-service database access creates tight coupling, makes independent deployment impossible, and violates the microservice architecture.

### 5. Let agents invent fields per PR

Rejected. Without a formal schema, agents will create inconsistent, overlapping, or contradictory field names across services. The data model must be the single source of truth.

### 6. Direct cross-database reads

Rejected. Game Engine reading Player DB (or vice versa) creates runtime coupling. If one service's schema changes, the other breaks. Snapshots and integration events provide loose coupling.

## Implementation roadmap

See [docs/data-model/10-migration-roadmap-from-current-state.md](../data-model/10-migration-roadmap-from-current-state.md) for the detailed migration plan.

Summary:

1. **data-model-0.1** (this PR) — Documentation and decisions only. No code changes.
2. **alpha-0.7.1** — Align Catalog schema with relational enemy/item/skill/law/curse definitions.
3. **alpha-0.7.2** — Align Player schema with permanent character stats and run snapshot.
4. **alpha-0.7.3** — Align Game Engine with runtime stat snapshots.
5. **alpha-0.7.4** — Migrate items/effects/modifiers to the official model.
6. **alpha-0.8.x** — Prepare ATB fields and runtime.
7. **alpha-0.9.x** — Security, exposition, gateway alignment.
