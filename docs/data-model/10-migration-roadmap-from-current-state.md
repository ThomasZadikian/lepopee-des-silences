# 10 — Migration roadmap from current state

Version: `data-model-0.1-rc2`

## Overview

This roadmap describes documentation and future implementation sequencing. This PR is documentation-only and must not create migrations or modify code.

## Product/version strategy

```text
0.6.x
→ gameplay stabilization: items, combat, rewards, modifiers, merchant, boss, laws/curses MVP.

data-model-0.1.x
→ official gameplay data model.

web-client-0.5.x
→ UI direction, combat scene, map, besace, rewards, responsive layout.

game-engine/catalog/player 0.7.x
→ data-driven backend implementation + Markov system foundation.

0.8.x
→ ATB, Interlude, Him'Lit, long narrative loop depending on readiness.

0.9.x
→ identity, gateway, security, observability, external alpha readiness.
```

Markov readiness must not be treated as alpha-0.9.x. Security/exposure belongs to alpha-0.9.x. Markov belongs to alpha-0.7.x after data-model and core data-driven schemas are accepted.

## Backend roadmap

```text
data-model-0.1
→ documentation and decisions.

alpha-0.7.1
→ Catalog relational schema aligned with data-model.

alpha-0.7.2
→ Player character stats and permanent progression aligned with data-model.

alpha-0.7.3
→ Game Engine run/combat snapshots aligned with data-model.

alpha-0.7.4
→ Official effects, items, rewards, laws, curses, modifiers.

alpha-0.7.5
→ Markov system foundation, versioned and deterministic.

alpha-0.7.6
→ Markov/adaptive selection integration for rooms, nodes, enemies, rewards, laws/curses.

alpha-0.7.7
→ Backend projections for Markov/Palace indicators usable by frontend.

alpha-0.8.x
→ ATB and narrative systems after the data-driven backend is stable.

alpha-0.9.x
→ security, gateway, externalization, observability, alpha-1 readiness.
```

## data-model-0.1 — documentation and decisions

Scope:

- harden data-model docs;
- update ADR-006;
- no code;
- no migration;
- no endpoint changes.

Gate: no Game Engine alpha-0.7.x implementation starts before data-model-0.1 is accepted.

## alpha-0.7.1 — Catalog relational schema

Target:

- enemy definitions and child stat blocks without circular 1:1 references;
- skill definitions with `effect_set_id`;
- item definitions with `effect_set_id`;
- law and curse definitions with `effect_set_id`;
- reward templates/options;
- EffectSet and EffectDefinition as the single source of effects;
- Catalog room definitions, room types, rare/cultural echo rooms, anomaly rooms, pools, mechanics, boss definitions;
- relational tag tables for Markov/adaptive filtering.

Do not create `catalog_skill_effects`, `catalog_item_effects`, `catalog_palace_law_effects`, or `catalog_curse_effects` as official target tables.

## alpha-0.7.2 — Player schema

Target:

- `auth_subject_id` as the Identity link;
- `player_character_stat_blocks` as 1:1 child rows;
- `player_character_skills` relational table;
- `player_permanent_unlocks`;
- future `player_run_statistics` including official metrics projected from Game Engine.

Player must not foreign-key to Identity.

## alpha-0.7.3 — Game Engine snapshots/runtime state

Target:

- run player and character snapshots;
- run inventory item snapshots with definition version and effect summary/key;
- combatant identity separated from base stat snapshots and runtime state;
- no `current_vitality` or `current_guard` in snapshot tables;
- reward offers/options persisted;
- active curses persisted;
- outbox remains integration boundary.

Target combat split:

```text
run_combatants
run_combatant_base_stat_snapshots
run_combatant_runtime_states
```

## alpha-0.7.4 — Effects, items, rewards, laws, curses, modifiers

Target:

- runtime effect resolution from EffectSet/EffectDefinition;
- RunModifier value mode and stack policy;
- item usage from RunItem snapshot;
- law and curse application from snapshotted definitions;
- official combat metrics fields on `run_combat_actions`;
- backend-authoritative data for future recaps and analytics.

## alpha-0.7.5 — Markov foundation

Target:

- versioned deterministic Markov/adaptive foundation;
- internal matrices remain confidential;
- seed/version tracking;
- no frontend raw probability exposure.

## alpha-0.7.6 — Adaptive selection integration

Target:

- room selection using Catalog room definitions and pools;
- node, enemy, reward, law, curse adaptive selection;
- runtime adaptive influences;
- selection decision traces without raw probabilities.

## alpha-0.7.7 — Palace projections

Target:

- `run_palace_pressure_snapshots`;
- `run_palace_indicator_snapshots`;
- frontend-safe narrative indicators.

## alpha-0.8.x — ATB and narrative systems

Target after data-driven backend is stable:

- ATB behavior from schema-ready fields;
- Interlude;
- Him'Lit;
- long narrative loop depending on readiness.

## alpha-0.9.x — External alpha readiness

Target:

- identity hardening;
- gateway/security;
- observability;
- externalization;
- alpha-1 readiness.

## Acceptance criteria for data-model-0.1

The correction is acceptable if:

- the roadmap indicates Markov in 0.7.x and not 0.9.x;
- Catalog rooms are modeled;
- rare rooms and cultural echo rooms are prepared;
- rare rooms do not use protected intellectual property;
- EffectSet is clearly the single source of Catalog effects;
- per-entity effect tables are removed or rejected;
- circular 1:1 stat block relations are removed;
- combatants separate identity, base snapshots, and mutable runtime values;
- `current_vitality` and `current_guard` are not in snapshot tables;
- RunItem snapshot is clarified;
- future official combat metrics are prepared;
- runtime Markov/Palace projections are prepared without exposing matrices;
- behavioral/generation effects are included;
- Identity ↔ Player is documented;
- naming inconsistencies are corrected;
- ADR-006 reflects the decisions;
- no code is modified;
- no migration is created;
- documents are coherent enough to serve as implementation references.
