# 02 — Combat stat taxonomy

Version: `data-model-0.1-rc2`

## Overview

This document defines canonical combat statistics and where each value is owned. Primary stats are explicit relational columns. Runtime mutable values are not stored in immutable snapshot tables.

## Stat lifecycle categories

| Category | Owner | Examples | Mutability |
|----------|-------|----------|------------|
| Catalog base stats | Catalog | enemy `max_vitality`, `attack_power` | Versioned definitions |
| Player permanent stats | Player | character `max_vitality`, `starting_guard` | Durable progression |
| Run snapshots | Game Engine | `run_character_stat_snapshots` | Immutable during run |
| Combat base snapshots | Game Engine | `run_combatant_base_stat_snapshots` | Immutable during combat |
| Combat runtime state | Game Engine | `current_vitality`, `current_guard` | Mutable during combat |
| Frontend display state | Frontend | animation deltas, hover target | Ephemeral, non-authoritative |

## Canonical stats

| Stat | Type | Owner/lifecycle | Description |
|------|------|-----------------|-------------|
| `max_vitality` | INT >= 1 | Catalog, Player, Game Engine snapshots | Maximum vitality/HP. Immutable inside a combat unless an explicit future effect changes max HP and runtime state together. |
| `current_vitality` | INT >= 0 | Game Engine runtime only | Mutable HP during combat/run. Must live in `run_combatant_runtime_states`, not a snapshot table. |
| `attack_power` | INT >= 0 | Catalog, Player, Game Engine snapshots | Base damage input. Can be modified by runtime effects through derived combat values. |
| `defense` | INT >= 0 | Catalog, Player, Game Engine snapshots | Damage mitigation input. |
| `starting_guard` | INT >= 0 | Catalog, Player, Game Engine base snapshots | Guard initialized at combat creation after modifiers. Immutable base value for the combat. |
| `current_guard` | INT >= 0 | Game Engine runtime only | Mutable guard during combat. Must live in `run_combatant_runtime_states`, not a snapshot table. |
| `speed` | INT >= 1 | Catalog, Player, Game Engine snapshots | Turn order today, future ATB fill input. |
| `initiative` | INT >= 0 | Catalog, Player, Game Engine snapshots | Future initial ATB position. Schema-ready only. |
| `recovery` | INT >= 0 | Catalog, Player, Game Engine snapshots | Future post-action recovery modifier. Schema-ready only. |
| `focus` | INT >= 0 | Catalog, Player, Game Engine base/runtime | Optional mental/secondary resource. Base value is snapshotted; current value is runtime. |
| `mana` | INT >= 0 | Player, Game Engine base/runtime | Current compatibility resource. Base value is snapshotted; current value is runtime. |
| `charge` | INT >= 0 | Player, Game Engine base/runtime | Current compatibility resource. Base value is snapshotted; current value is runtime. |
| `atb_ready_threshold` | INT >= 1 | Game Engine base snapshot | Future threshold for readiness. Schema-ready only. |
| `atb_gauge_value` | INT >= 0 | Game Engine runtime only | Future mutable ATB gauge. Schema-ready only. |
| `action_recovery_until_tick` | INT NULL | Game Engine runtime only | Future mutable ATB recovery marker. Schema-ready only. |

## Guard naming

- `starting_guard`: immutable base/start value for a combat after run modifiers are applied.
- `current_guard`: mutable value that absorbs damage during combat.
- `base_guard`: legacy/current implementation name. Do not use as target schema name.

`starting_guard` is computed from snapshots plus active modifiers at combat creation. It is never recomputed from a previous combat runtime value.

## Combatant table placement

```text
run_combatants
  identity only: source_key, display_name, side, archetype, status

run_combatant_base_stat_snapshots
  max_vitality, attack_power, defense, starting_guard, speed, initiative, recovery,
  focus, mana, charge, atb_ready_threshold

run_combatant_runtime_states
  current_vitality, current_guard, current_focus, current_mana, current_charge,
  atb_gauge_value, action_recovery_until_tick
```

## Damage terminology

| Term | Meaning |
|------|---------|
| `raw_damage` | Amount before mitigation and guard. |
| `mitigated_damage` | Amount after defense/resistance mitigation. |
| `guard_absorbed` | Amount absorbed by guard. |
| `guard_damage` | Amount removed from current_guard. Usually equals `guard_absorbed`. |
| `vitality_damage` | Amount removed from current_vitality. |
| `damage_taken` | Target-side metric, usually vitality damage unless explicitly including guard. |

Official metrics must be backend-authoritative in `run_combat_actions`. Frontend meters are display-only until backed by these fields.

## Naming corrections

- Use `PlayerCharacterStatBlock`, not `PlayerCharacterStat_block`.
- Use `skill_definition_key` for references to Catalog skill keys in relational rows.
- Use `key` for the primary stable key of Catalog definition tables.
- Use `targeting_mode` in Catalog definitions and `targeting_type` only for current DTO compatibility.
