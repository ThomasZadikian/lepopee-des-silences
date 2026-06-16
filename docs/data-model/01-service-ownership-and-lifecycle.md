# 01 — Service ownership and lifecycle

Version: `data-model-0.1-rc2`

## Ownership rules

```text
Catalog owns stable definitions.
Player owns permanent player progression.
Game Engine owns runtime snapshots and combat state.
Frontend displays state and never owns gameplay truth.
```

No service uses cross-database foreign keys. Cross-service references use stable ids, definition keys, versions, snapshots, or integration events.

## Entity ownership matrix

| Entity | Owner | Lifecycle | Target state |
|--------|-------|-----------|--------------|
| EnemyDefinition | Catalog | Durable, versioned | `catalog_enemy_definitions` |
| EnemyStatBlock | Catalog | Durable 1:1 child of enemy | `catalog_enemy_stat_blocks.enemy_definition_id UNIQUE` |
| SkillDefinition | Catalog | Durable, versioned | `catalog_skill_definitions` with `effect_set_id` |
| ItemDefinition | Catalog | Durable, versioned | `catalog_item_definitions` with `effect_set_id` |
| EffectSet | Catalog | Durable, versioned | canonical effect source |
| EffectDefinition | Catalog | Durable child of EffectSet | `catalog_effect_definitions` |
| PalaceLawDefinition | Catalog | Durable, versioned | `catalog_palace_law_definitions` with `effect_set_id` |
| CurseDefinition | Catalog | Durable, versioned | `catalog_curse_definitions` with `effect_set_id` |
| RewardTemplate | Catalog | Durable, versioned | `catalog_reward_templates` |
| RewardTemplateOption | Catalog | Durable option | may reference `effect_set_id` |
| RoomDefinition | Catalog | Durable, versioned | `catalog_room_definitions` |
| RoomTypeDefinition | Catalog | Durable, versioned | `catalog_room_type_definitions` |
| RoomEnemyPool | Catalog | Durable selection pool | `catalog_room_enemy_pools` |
| RoomRewardPool | Catalog | Durable selection pool | `catalog_room_reward_pools` |
| RoomLawPool | Catalog | Durable selection pool | `catalog_room_law_pools` |
| RoomCursePool | Catalog | Durable selection pool | `catalog_room_curse_pools` |
| RoomSpecialMechanic | Catalog | Durable mechanic definition | may reference `effect_set_id` |
| RoomBossDefinition | Catalog | Durable boss definition | `catalog_room_boss_definitions` |
| PlayerProfile | Player | Durable per player | optional auth subject link |
| PlayerCharacter | Player | Durable per player | identity and display only |
| PlayerCharacterStatBlock | Player | Durable 1:1 child | `player_character_id UNIQUE` |
| PlayerCharacterSkill | Player | Durable per character | relational skill keys |
| PermanentUnlock | Player | Durable per player | projected via outbox |
| Run | Game Engine | Durable during run | root runtime aggregate |
| RunCharacterSnapshot | Game Engine | Immutable during run | run-start player snapshot |
| RunItem | Game Engine | Stable during run | runtime item snapshot |
| RunModifier | Game Engine | Mutable run runtime | effect-derived runtime modifier |
| ActivePalaceLaw | Game Engine | Mutable run runtime | snapshot of accepted law |
| ActiveCurse | Game Engine | Mutable run runtime | snapshot of applied curse |
| Combat | Game Engine | Durable during combat | combat root |
| Combatant | Game Engine | Durable combat identity | no duplicated stats |
| CombatantBaseStatSnapshot | Game Engine | Immutable combat snapshot | base values at combat creation |
| CombatantRuntimeState | Game Engine | Mutable combat state | current values only |
| CombatAction | Game Engine | Durable audit/metrics | future authoritative metrics |
| AdaptiveInfluence | Game Engine | Runtime adaptive trace | qualitative, no matrix exposure |
| PalaceIndicatorSnapshot | Game Engine | Runtime display projection | frontend-safe indicator |
| Frontend meter state | Frontend | Ephemeral display | non-authoritative |

## Lifecycle rules

### Catalog definitions

```text
Lifecycle: Draft -> Active -> Deprecated -> Disabled
Versioning: explicit version string on each definition
Mutability: edits require versioning when behavior can change
Deletion: definitions are disabled, not hard-deleted
```

Catalog definitions are global. Game Engine snapshots the definition key, version, display information, stats, and effect summaries it needs at acquisition, run start, combat start, or reward offer creation.

### Player permanent state

```text
Lifecycle: Created -> Active
Mutability: evolves between runs or via accepted integration events
Deletion: privacy/GDPR policy applies outside gameplay data model
```

Player permanent stats do not change active runs. Future progression affects subsequent runs unless a specific Game Engine integration event is accepted and snapshotted.

### Game Engine runtime state

```text
Run lifecycle: Created -> Active -> Completed/Failed/Abandoned/Suspended
Combat lifecycle: Pending -> Active -> Completed/Failed
Snapshot mutability: immutable after creation
Runtime mutability: mutable only inside Game Engine
```

Game Engine does not live-read Catalog or Player while resolving combat. It uses snapshots and runtime state.

## Snapshot rules

| Source | Snapshot target | When | Why |
|--------|----------------|------|-----|
| PlayerProfile | `run_player_snapshots` | Run start | Stable run owner/display state |
| PlayerCharacter | `run_character_snapshots` | Run start | Stable character identity |
| PlayerCharacterStatBlock | `run_character_stat_snapshots` | Run start | Stable base stats during run |
| PlayerCharacterSkill | `run_character_skill_snapshots` | Run start | Stable skill availability |
| ItemDefinition | `run_inventory_items` | Item acquisition | Stable item display/behavior during run |
| EnemyDefinition | `run_combatants` + base stat snapshot | Combat creation | Stable combatant identity and base values |
| EnemyStatBlock | `run_combatant_base_stat_snapshots` | Combat creation | Stable computed stats |
| SkillDefinition | `run_combatant_skills` | Combat creation | Stable skill power/cost/effect summary |
| Law/Curse definition | `run_active_laws` / `run_active_curses` | Application | Stable display and runtime behavior |
| RewardTemplateOption | `run_reward_options` | Offer creation | Stable choices despite Catalog changes |

## RunItem snapshot rule

Catalog owns item definitions. Game Engine owns runtime item snapshots. A `RunItem` is not a permanent inventory model; permanent inventory or unlocks belong to Player or a future dedicated service if the domain grows.

`run_inventory_items` must snapshot enough information for the item to remain stable after acquisition:

```text
definition_key
definition_version
narrative_text
item_type
category
rarity
usage_mode
lifecycle
quantity
max_stack
effect_set_key or effect_summary
is_usable_in_combat
is_usable_outside_combat
source_reward_option_id
acquired_at_utc
```

An item acquired during a run must not change display text or immediate behavior because Catalog changed after acquisition.

## Combatant state split

Combat data uses three conceptual layers:

```text
run_combatants
-> identity of a combatant in a combat.

run_combatant_base_stat_snapshots
-> immutable values calculated when combat is created.

run_combatant_runtime_states
-> mutable current combat values.
```

Rules:

- `run_combatants` does not duplicate stats.
- `current_vitality` is not stored in a table named snapshot.
- `current_guard` is not stored in a table named snapshot.
- Snapshot values are frozen.
- Runtime values are mutable.

## One-to-one relation rule

For 1:1 stat blocks, the parent does not carry the child id. The child carries the parent id with a UNIQUE constraint.

Example:

```text
catalog_enemy_definitions
- id PK
- key
- ...

catalog_enemy_stat_blocks
- id PK
- enemy_definition_id UUID UNIQUE NOT NULL FK
- max_vitality
- attack_power
- ...
```

The same rule applies to:

- PlayerCharacter -> PlayerCharacterStatBlock;
- RunCharacterSnapshot -> RunCharacterStatSnapshot;
- Combatant -> CombatantBaseStatSnapshot;
- Combatant -> CombatantRuntimeState.

## Identity relationship

PlayerProfile must be able to link to an authenticated identity without depending on the Identity database.

Target optional field:

```text
auth_subject_id VARCHAR(160) NULL
```

Rules:

- Player does not foreign-key to Identity.
- Player stores only a stable authentication subject id.
- Identity owns credentials, MFA, sessions, security, and account lifecycle.
- Player owns gameplay profile and progression.

`auth_subject_id` is the target field name for data-model-0.1.

## Frontend rule

The frontend may display transient state such as hover targets, local combat animation, and local meters. It must not become the source of gameplay truth. Backend DTOs, runtime snapshots, actions, and future metrics are authoritative.
