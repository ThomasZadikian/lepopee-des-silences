# ADR-006 — Gameplay entity ownership and relational schema

## Status

Proposed for `data-model-0.1-rc2`. Not accepted yet.

## Date

2026-06-16

## Context

The project has a playable alpha loop, but gameplay data is split across Catalog, Player, Game Engine, frontend display state, JSON columns, hardcoded definitions, and runtime aggregates. Before Game Engine alpha-0.7.x starts, the project needs a single reference for ownership, relational shape, snapshots, effects, rooms, Markov readiness, ATB readiness, and future metrics.

The corrective data-model-0.1 pass must remain documentation-only. It must not create migrations, change code, modify endpoints, or implement Markov/ATB/rare rooms.

## Decision

1. The project adopts explicit relational schemas for gameplay core data.
2. Catalog owns stable, versioned definitions.
3. Player owns permanent player progression.
4. Game Engine owns runtime snapshots and mutable combat state.
5. Frontend displays state and never owns gameplay truth.
6. Primary gameplay stats are explicit relational columns, not JSON.
7. Inter-service links use stable keys and integration events, not cross-database foreign keys.
8. EffectSet is the canonical source of Catalog effects.
9. Skill, Item, PalaceLaw, Curse, RewardTemplateOption when needed, and RoomSpecialMechanic when needed reference `effect_set_id`.
10. Per-entity duplicated effect tables are not part of data-model-0.1.
11. Runtime combat state separates combatant identity, immutable base stat snapshots, and mutable runtime state.
12. `current_vitality` and `current_guard` belong to runtime state, not tables named snapshot.
13. One-to-one stat block relations are parent-to-child: the stat block carries the parent id with a UNIQUE constraint. Parent tables do not also carry `stat_block_id`.
14. RunItems snapshot enough Catalog item data to stay stable during a run.
15. Rare rooms and cultural echo rooms are first-class Catalog definitions.
16. Rare/cultural echo rooms can use abstract cultural, mythological, literary, or symbolic inspiration, but must not copy protected names, characters, organizations, symbols, or terminology from existing works.
17. Markov internals remain confidential. Runtime qualitative projections and traces are allowed.
18. The frontend never displays raw Markov matrices or raw probabilities.
19. ATB fields are schema-ready but not behaviorally active yet.
20. Future official combat metrics are backend-authoritative relational data. Current frontend meters remain non-authoritative.
21. Player may link to Identity through `auth_subject_id`, but Player must not foreign-key to the Identity database.
22. No Game Engine alpha-0.7 implementation starts before data-model-0.1 is accepted.

## Consequences

Positive consequences:

- The backend implementation sequence can proceed without inventing new ownership rules per PR.
- Data-driven Catalog, Player permanent progression, Game Engine runtime snapshots, and frontend display state become clearly separated.
- Markov/adaptive selection can be prepared in alpha-0.7.x without exposing confidential internals.
- ATB can be prepared in schema without activating behavior prematurely.
- Future combat recaps, run recaps, achievements, balancing, analytics, and post-combat UI can use backend metrics.

Negative consequences:

- The target schema is larger than the current implementation.
- Some current JSON columns need relational migration later.
- Some current hardcoded/InMemory definitions need Catalog persistence later.
- Existing documentation and implementations must align with EffectSet as the single effect source.

## Alternatives rejected

- Primary stats in JSON.
- A single global gameplay database.
- Catalog as the source of runtime state.
- Game Engine as the source of stable definitions.
- Player as the owner of combat runtime values.
- Per-entity duplicated effect tables such as `catalog_skill_effects`, `catalog_item_effects`, `catalog_palace_law_effects`, and `catalog_curse_effects`.
- Direct frontend exposure of Markov matrices or raw probabilities.
- Hardcoded rare rooms in Game Engine.
- Cross-database foreign keys between services.
- Starting alpha-0.7.x implementation before data-model-0.1 is accepted.

## Implementation roadmap

See `docs/data-model/10-migration-roadmap-from-current-state.md`.

Summary:

1. `data-model-0.1` — documentation and decisions.
2. `alpha-0.7.1` — Catalog relational schema aligned with data-model.
3. `alpha-0.7.2` — Player character stats and permanent progression aligned with data-model.
4. `alpha-0.7.3` — Game Engine run/combat snapshots aligned with data-model.
5. `alpha-0.7.4` — Official effects, items, rewards, laws, curses, modifiers.
6. `alpha-0.7.5` — Markov system foundation, versioned and deterministic.
7. `alpha-0.7.6` — Markov/adaptive selection integration for rooms, nodes, enemies, rewards, laws/curses.
8. `alpha-0.7.7` — Backend projections for Markov/Palace indicators usable by frontend.
9. `alpha-0.8.x` — ATB and narrative systems after the data-driven backend is stable.
10. `alpha-0.9.x` — security, gateway, externalization, observability, alpha-1 readiness.
