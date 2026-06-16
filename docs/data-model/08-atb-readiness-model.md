# 08 — ATB readiness model

Version: `data-model-0.1-rc2`

## Purpose

This document defines schema readiness for a future Active Time Battle system. ATB is not implemented by data-model-0.1. The fields are target schema fields only and must not be treated as behaviorally active until a later implementation PR.

## Current combat mode

The current combat system remains turn-based/round-robin. The schema prepares ATB without changing combat behavior.

## Catalog fields

### SkillDefinition

```text
action_cost INT NOT NULL DEFAULT 10
cast_time INT NOT NULL DEFAULT 0
recovery_time INT NOT NULL DEFAULT 0
```

These fields are snapshotted into `run_combatant_skills` at combat creation.

## Player and Catalog stat fields

`initiative` and `recovery` exist on:

- `catalog_enemy_stat_blocks`;
- `player_character_stat_blocks`;
- `run_character_stat_snapshots`;
- `run_combatant_base_stat_snapshots`.

They are immutable base/snapshot values during a combat.

## Game Engine combatant split

ATB fields follow the identity/base/runtime split.

```text
run_combatants
-> identity only.

run_combatant_base_stat_snapshots
-> initiative, recovery, atb_ready_threshold.

run_combatant_runtime_states
-> atb_gauge_value, action_recovery_until_tick.
```

`atb_gauge_value` and `action_recovery_until_tick` are mutable runtime fields and must not live in a snapshot table.

## Conceptual formulas

These formulas are illustrative, not accepted behavior.

```text
atb_fill_rate = base_rate + (speed * speed_factor) + modifiers
```

```text
When combatant acts:
  deduct action_cost from atb_gauge_value
  wait cast_time if non-zero
  resolve skill
  action_recovery = skill.recovery_time + combatant.recovery + modifiers
  action_recovery_until_tick = current_tick + action_recovery
```

## Guard timing

Current guard behavior remains round-based. Future ATB can define guard reset by tick interval, action cycle, or explicit effect duration. That decision is out of scope for data-model-0.1.

## Migration positioning

- alpha-0.7.x adds data-driven schemas and ATB-ready columns where needed.
- alpha-0.8.x may implement ATB after the data-driven backend is stable.
- alpha-0.9.x is not the ATB foundation phase; it is reserved for security, gateway, observability, externalization, and alpha-1 readiness.

## Rules

- ATB fields may be nullable or defaulted while inactive.
- No ATB behavior should be implemented as part of data-model-0.1.
- Current round-robin combat can coexist with ATB-ready schema fields.
- Future official ATB behavior requires a dedicated ADR or combat system document.
