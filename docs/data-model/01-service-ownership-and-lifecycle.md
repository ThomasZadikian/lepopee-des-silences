# 01 — Service ownership and lifecycle

## Entity ownership matrix

| Entity | Owner | Lifecycle | Current state |
|--------|-------|-----------|---------------|
| `EnemyDefinition` | Catalog | Durable, versioned, stable | EF-persisted (`catalog_enemy_definitions`) |
| `EnemyStatBlock` (target) | Catalog | Durable, attached to definition | Not yet separate table |
| `SkillDefinition` | Catalog | Durable, versioned, stable | EF-persisted (`catalog_skill_definitions`) |
| `ItemDefinition` | Catalog | Durable, versioned, stable | EF-persisted (`catalog_item_definitions`) |
| `PalaceLawDefinition` | Catalog | Durable, versioned, stable | EF-persisted (`catalog_palace_law_definitions`) |
| `CurseDefinition` (target) | Catalog | Durable, versioned, stable | Not yet exists |
| `RoomBossDefinition` | Catalog | Durable, versioned, stable | InMemory only |
| `EventTemplate` | Catalog | Durable, versioned, stable | InMemory only |
| `RewardTemplate` (target) | Catalog | Durable, versioned, stable | Not yet exists |
| `EffectDefinition` (target) | Catalog | Durable, composable | Not yet exists |
| `PlayerProfile` | Player | Durable, per-player | EF-persisted (`player_profiles`) |
| `PlayerCharacter` | Player | Durable, per-player | EF-persisted (`player_characters`) |
| `PlayerCharacterStatBlock` (target) | Player | Durable, attached to character | Not yet separate table |
| `PlayerCharacterSkill` (target) | Player | Durable, attached to character | JSON column (`skill_keys_json`) |
| `PlayerProgression` | Player | Durable, per-player | Embedded in `player_profiles` |
| `PermanentUnlock` (target) | Player | Durable, per-player | Not yet exists |
| `Run` | Game Engine | Durable during run, archivable | EF-persisted (`runs`) |
| `Room` | Game Engine | Durable during run | EF-persisted (`run_rooms`) |
| `MapNode` | Game Engine | Durable during run | EF-persisted (`run_nodes`) |
| `RunCharacterSnapshot` (target) | Game Engine | Durable during run | Not yet separate table |
| `RunCombatantStatSnapshot` (target) | Game Engine | Durable during combat | Not yet separate table |
| `RunItem` | Game Engine | Durable during run | EF-persisted (`run_items`) |
| `RunModifier` | Game Engine | Durable during run | EF-persisted (`run_modifiers`) |
| `ActivePalaceLaw` | Game Engine | Durable during run | EF-persisted (`run_active_palace_laws`) |
| `ActiveCurse` (target) | Game Engine | Durable during run | Not yet separate table |
| `Combat` | Game Engine | Durable during combat | EF-persisted (`run_active_combats`) |
| `Combatant` | Game Engine | Durable during combat | EF-persisted (`run_combatants`) |
| `CombatantSkill` | Game Engine | Durable during combat | EF-persisted (`run_combatant_skills`) |
| `RewardOffer` | Game Engine | Durable during reward selection | InMemory only |
| `OutboxMessage` | Game Engine | Durable until dispatched | EF-persisted (`game_engine_outbox_messages`) |

## Lifecycle rules

### Catalog definition

```text
Lifecycle: Draft → Active → Deprecated → Disabled
Versioning: explicit version string on each definition
Mutability: administrators may update; version bumps on breaking changes
Deletion: definitions are never hard-deleted; they are Disabled
```

- Catalog definitions are global and shared across all players and all runs.
- A definition's `key` + `version` uniquely identifies a specific revision.
- Game Engine snapshots the `key` at combat creation. It does not track `version` changes mid-combat.

### Player permanent state

```text
Lifecycle: Created → Active (forever)
Mutability: stats evolve between runs via outbox events
Deletion: player data is never deleted (GDPR applies separately)
```

- Player characters have permanent base stats (`max_vitality`, `attack_power`, `defense`, etc.).
- These stats are snapshotted into Game Engine at run start.
- Changes to permanent stats (future: leveling, unlocks) take effect on the next run.

### Game Engine runtime state

```text
Lifecycle: Created → Active → Completed/Failed/Abandoned/Suspended
Archivability: run data may be archived or pruned after terminal state
Combat lifecycle: Pending → Active → Completed/Failed
```

- Run state persists for the duration of the run.
- Combat state persists for the duration of the combat encounter.
- Snapshots are frozen at creation time. Catalog or Player changes do not propagate to active runs.

## Snapshot rules

### What gets snapshotted

| Source | Snapshot target | When | Why |
|--------|----------------|------|-----|
| `PlayerCharacter` permanent stats | `run_character_stat_snapshots` | Run start | Prevent mid-run stat changes from affecting active run |
| `PlayerCharacter` skill keys | `run_character_skill_snapshots` | Run start | Prevent mid-run skill changes |
| `EnemyDefinition` base stats | `run_combatant_stat_snapshots` | Combat creation | Prevent Catalog changes from affecting active combat |
| `EnemyDefinition` skill keys | `run_combatant_skills` | Combat creation | Freeze enemy combat capabilities |
| `SkillDefinition` stats | `run_combatant_skills` | Combat creation | Freeze skill power/cost |
| `RunModifier` active bonuses | Applied to combatant stats | Combat creation | Aggregate all active modifiers into starting values |

### What is NOT snapshotted

| Source | Why not |
|--------|---------|
| `ItemDefinition` full schema | Only `definition_key` is stored; details looked up from snapshot at use time |
| `PalaceLawDefinition` full schema | Only `key` and display info stored; effects already converted to `RunModifier` |
| `EventTemplate` full schema | Resolved at event time; no persistent snapshot needed |

### Snapshot anti-pattern: the guard doubling bug

A known bug occurred where `starting_guard` bonuses doubled when changing rooms. The root cause was:

1. `RunModifier(StartingGuardBonus)` was persisted on the run.
2. On room transition, the runtime recalculated `starting_guard` from the modifier.
3. The modifier was then re-applied on top of the already-applied value.

**Design rule:** `starting_guard` is computed as:

```text
effective_starting_guard = character_base_starting_guard + SUM(active StartingGuardBonus modifiers)
```

This computation must be idempotent. The base value comes from the snapshot. The modifiers are read once and summed. The result is written to the combatant's `starting_guard` field. The modifiers themselves are NOT consumed (they persist until their duration expires). The combatant's `current_guard` is initialized to `starting_guard` at combat creation and reset to `base_guard` (floor) at each new round.

## Ownership: detailed breakdown

### ItemDefinition → Catalog

```text
Owner: Catalog
Lifecycle: Draft → Active → Deprecated → Disabled
Key: definition_key (e.g., "item.consumable.baume-de-memoire")
Snapshot: Game Engine copies definition_key + display info into run_items at acquisition
Permanent: future PermanentInventoryItem → Player (via outbox)
```

### EnemyDefinition → Catalog

```text
Owner: Catalog
Lifecycle: Draft → Active → Deprecated → Disabled
Key: definition_key (e.g., "enemy.threshold.doubt-fragment")
Snapshot: Game Engine creates run_combatant from definition + difficulty multiplier
Runtime: Combatant mutable state (current_vitality, current_guard) lives in Game Engine only
```

### PlayerCharacter → Player

```text
Owner: Player
Lifecycle: Created → Active (forever)
Key: character_id (UUID) + definition_key (e.g., "character.player.self")
Snapshot: Game Engine copies permanent stats into run_character_stat_snapshots at run start
Runtime: CombatantRuntimePlayer (mutable) lives in Game Engine only
```

### SkillDefinition → Catalog

```text
Owner: Catalog
Lifecycle: Draft → Active → Deprecated → Disabled
Key: definition_key (e.g., "skill.basic.strike")
Snapshot: Game Engine copies skill stats into run_combatant_skills at combat creation
Permanent: future UnlockedSkill → Player (via player_character_skills)
```

### PalaceLawDefinition → Catalog

```text
Owner: Catalog
Lifecycle: Draft → Active → Deprecated → Disabled
Key: definition_key (e.g., "law.threshold.silence-weight")
Runtime: ActivePalaceLaw + RunModifier → Game Engine (per-run)
Permanent: future PermanentLawUnlock → Player
```

### CurseDefinition → Catalog

```text
Owner: Catalog
Lifecycle: Draft → Active → Deprecated → Disabled
Key: definition_key (e.g., "curse.threshold.souffle-lourd")
Runtime: ActiveCurse + RunModifier → Game Engine (per-run)
```
