# 08 — ATB readiness model

## Purpose

This document defines the data fields and conceptual formulas required for a future Active Time Battle (ATB) system. **ATB is not implemented in this PR.** This document only specifies what the data model must support.

## Why prepare for ATB now

The current combat system uses simple round-robin turn order: allies act first, then enemies, in a fixed sequence. This works but is mechanically flat. A future ATB system will add:

- **Speed-based turn frequency.** Faster combatants act more often.
- **Action commitment.** Using a skill consumes time (recovery), creating vulnerability windows.
- **Interruptibility.** Long cast times can be interrupted.
- **Tactical depth.** Players must consider when to act, not just what to do.

By including ATB-ready fields in the schema now, we avoid a costly migration later.

## Current fields (already exist)

These fields exist in the current domain model and are already in the schema:

| Field | Entity | Current use | ATB use |
|-------|--------|------------|---------|
| `speed` | `Combatant`, `Run`, `PlayerCharacter` | Round-robin ordering (DESC) | `atb_fill_rate` input |
| `attack_power` | `Combatant`, `Run` | Damage calculation | Unchanged |
| `defense` | `Combatant`, `Run` | Damage mitigation | Unchanged |
| `current_guard` | `Combatant` | Guard absorption | Reset timing tied to ATB rounds |
| `current_vitality` | `Combatant` | HP tracking | Unchanged |
| `max_vitality` | `Combatant` | HP cap | Unchanged |

## New fields required (schema-only in this PR)

### On Catalog (SkillDefinition)

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `action_cost` | INT | 10 | Ticks consumed from ATB gauge when skill is used |
| `cast_time` | INT | 0 | Ticks before skill resolves (0 = instant) |
| `recovery_time` | INT | 0 | Ticks added to combatant recovery after skill use |

### On Game Engine (run_combatant_stat_snapshots)

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `initiative` | INT | 0 | Starting position in ATB timeline |
| `recovery` | INT | 0 | Base recovery modifier (added to skill recovery_time) |
| `atb_gauge_value` | INT | NULL | Current position in ATB timeline |
| `atb_ready_threshold` | INT | NULL | Threshold to act (typically 1000) |
| `action_recovery_until_tick` | INT | NULL | Tick when combatant can act again |

### On Game Engine (run_combatant_effects)

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `created_at_tick` | INT | NULL | ATB tick when effect was created |
| `expires_at_tick` | INT | NULL | ATB tick when effect expires |

## Conceptual ATB formulas

### ATB fill rate

```text
atb_fill_rate = base_rate + (speed * speed_factor) + modifiers
```

Where:
- `base_rate` is a design constant (e.g., 100)
- `speed_factor` is a tuning constant (e.g., 10)
- `modifiers` = SUM of `ModifySpeed` RunModifiers applied to fill rate

### ATB tick advancement

```text
Each game tick:
  for each combatant:
    if combatant.action_recovery_until_tick > current_tick:
      skip (still recovering)
    else:
      combatant.atb_gauge_value += atb_fill_rate
      if combatant.atb_gauge_value >= atb_ready_threshold:
        combatant is ready to act
```

### Action resolution

```text
When combatant acts:
  1. Select skill
  2. Deduct action_cost from atb_gauge_value
  3. Begin cast_time countdown (if > 0)
  4. Resolve skill effects
  5. Compute action_recovery = skill.recovery_time + combatant.recovery + modifiers
  6. Set action_recovery_until_tick = current_tick + action_recovery
```

### Guard reset timing

```text
Current: reset at start of each "round" (all combatants acted once)
ATB: reset at fixed tick intervals (e.g., every 1000 ticks) or per "round" defined as
     a full cycle of all combatants acting
```

## Design notes

- The exact formulas will be decided in a future ADR dedicated to combat system v2.
- `atb_ready_threshold` is a constant (e.g., 1000) rather than a per-combatant stat. It can be made per-combatant later if needed.
- `action_recovery_until_tick` replaces the current "next actor" model. The combatant with the lowest `action_recovery_until_tick` whose gauge is full acts next.
- Cast time creates a window where the combatant can be interrupted. Interruption mechanics are out of scope for this document.
- The current round-robin system can coexist with ATB during migration. A feature flag can switch between the two.

## Migration path

1. **alpha-0.7.x:** Add `initiative`, `recovery` columns to `catalog_enemy_stat_blocks` and `player_character_stat_blocks`. Default to 0. No behavioral change.
2. **alpha-0.7.x:** Add `action_cost`, `cast_time`, `recovery_time` columns to `catalog_skill_definitions`. Default to current implicit values.
3. **alpha-0.7.x:** Add `atb_gauge_value`, `atb_ready_threshold`, `action_recovery_until_tick` columns to `run_combatant_stat_snapshots`. Nullable, no behavioral change.
4. **alpha-0.8.x:** Implement ATB tick loop alongside round-robin. Feature flag selects which system is active.
5. **alpha-0.8.x:** Add `created_at_tick`, `expires_at_tick` to `run_combatant_effects`. Nullable.
6. **alpha-0.9.x:** Remove round-robin code path once ATB is stable.
