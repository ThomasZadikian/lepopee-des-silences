# 09 — Markov readiness model

Version: `data-model-0.1-rc2`

## Purpose

This document defines schema readiness for future Markov/adaptive selection. It does not document internal matrices, raw probabilities, or confidential algorithms, and it does not implement Markov.

Markov readiness belongs to alpha-0.7.x after data-model and core data-driven schemas are accepted. It must not be deferred to alpha-0.9.x.

## Non-disclosure rules

- Do not document internal matrices.
- Do not expose raw probabilities.
- Do not describe confidential transition algorithms.
- Do not expose matrix internals to frontend.
- Store and expose only selected keys, versions, seeds, qualitative influences, and narrative indicators.

## Catalog adaptive metadata

Catalog provides candidates and stable metadata. Game Engine owns selection context and decisions.

Common fields on selectable Catalog definitions:

```text
key
tags via relational tables
base_weight
min_depth
max_depth
selection_group
family / role where relevant
theme / room_family / room_rarity where relevant
```

Participating entities:

- EnemyDefinition;
- SkillDefinition where selection is needed;
- ItemDefinition;
- PalaceLawDefinition;
- CurseDefinition;
- RewardTemplate and RewardTemplateOption;
- RoomDefinition;
- RoomTypeDefinition;
- RoomEnemyPool;
- RoomRewardPool;
- RoomLawPool;
- RoomCursePool;
- RoomSpecialMechanic;
- RoomBossDefinition.

## Catalog room readiness

Room definitions are first-class Markov/adaptive candidates. The Catalog schema includes:

```text
catalog_room_definitions
catalog_room_type_definitions
catalog_room_enemy_pools
catalog_room_enemy_pool_entries
catalog_room_reward_pools
catalog_room_reward_pool_entries
catalog_room_law_pools
catalog_room_law_pool_entries
catalog_room_curse_pools
catalog_room_curse_pool_entries
catalog_room_special_mechanics
catalog_room_tags
catalog_room_boss_definitions
```

RoomDefinition fields relevant to Markov/adaptive selection:

```text
room_family
room_rarity
theme
min_depth
max_depth
base_weight
selection_group
enemy_pool_key
reward_pool_key
law_pool_key
curse_pool_key
special_mechanic_key
boss_definition_key
is_unique
is_cultural_echo
```

## Rare rooms and cultural echo rooms

Rare rooms may evoke cultural, mythological, literary, or symbolic archetypes. They must not copy protected intellectual property.

Allowed:

- abstract inspiration;
- broad symbolic theme;
- public-domain archetypes;
- generic alchemical, mythological, literary, or philosophical motifs.

Forbidden:

- protected work names;
- character names;
- organization names;
- recognizable symbols;
- protected terminology;
- direct one-to-one recreation of existing fictional rooms.

Example:

```text
Room key: room.rare.creuset-equivalences
Display name: Le Creuset des Equivalences
RoomFamily: CulturalEcho
RoomRarity: Rare
Theme: Alchemical
Special mechanic: equivalent_exchange
Design intent: Une salle ou chaque puissance offerte exige une dette.
Allowed inspiration: alchemy, equivalent exchange, body debt, artificial life, guilt.
Forbidden: direct references to protected works, character names, organization names, recognizable symbols.
```

## Runtime adaptive projections

Game Engine stores runtime traces and projections, not raw matrix internals.

### run_adaptive_influences

```text
id UUID PK
run_id UUID NOT NULL FK
source_type VARCHAR(64) NOT NULL
source_key VARCHAR(160) NOT NULL
influence_type VARCHAR(64) NOT NULL
influence_tag VARCHAR(128) NOT NULL
value DECIMAL(10,4) NULL
value_mode VARCHAR(32) NULL
duration VARCHAR(64) NOT NULL
created_at_utc TIMESTAMPTZ NOT NULL
consumed_at_utc TIMESTAMPTZ NULL
```

Examples:

- `behavior.paranoid`;
- `generation.alchemical`;
- `pressure.hostile`;
- `reward.defensive_bias`;
- `enemy.echo_bias`.

### run_selection_decisions

```text
id UUID PK
run_id UUID NOT NULL FK
decision_type VARCHAR(64) NOT NULL
context_key VARCHAR(160) NULL
selected_key VARCHAR(160) NOT NULL
selection_group VARCHAR(64) NULL
matrix_version VARCHAR(64) NULL
seed VARCHAR(128) NOT NULL
created_at_utc TIMESTAMPTZ NOT NULL
```

Do not store raw probabilities by default.

### run_palace_pressure_snapshots

Future qualitative pressure snapshot for backend and frontend-safe summaries. It may store pressure key, intensity, dominant tag, display label, narrative text, and expiration.

### run_palace_indicator_snapshots

```text
id UUID PK
run_id UUID NOT NULL FK
indicator_key VARCHAR(160) NOT NULL
display_label VARCHAR(256) NOT NULL
narrative_text TEXT NOT NULL
intensity VARCHAR(64) NOT NULL
source_decision_id UUID NULL
created_at_utc TIMESTAMPTZ NOT NULL
expires_at_utc TIMESTAMPTZ NULL
```

Frontend examples:

- Le Palais se crispe.
- Les echos deviennent mefiants.
- Une salle instable approche.
- La Loi pese sur les choix du Palais.

## Behavioral/generation effects

EffectDefinitions may create adaptive influences through:

```text
ModifyEnemyBehavior
ModifyTargetingBias
ModifyGenerationWeight
ModifyRoomSelectionBias
ModifyEnemySelectionBias
ModifyRewardSelectionBias
ModifyLawSelectionBias
ModifyCurseSelectionBias
ApplyBehaviorTag
ApplyNarrativePressure
```

Tags that influence selection must be explicit columns or relational tags, not hidden JSON.

## Alpha positioning

- alpha-0.7.5: Markov foundation, versioned and deterministic.
- alpha-0.7.6: adaptive selection integration for rooms, nodes, enemies, rewards, laws/curses.
- alpha-0.7.7: frontend-safe Markov/Palace projections.
- alpha-0.9.x: security, gateway, externalization, observability, alpha-1 readiness.
