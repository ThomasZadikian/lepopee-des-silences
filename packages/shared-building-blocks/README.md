# Leds.SharedBuildingBlocks

Shared technical building blocks for L’épopée des silences services.

## Purpose

This package contains stable, cross-service technical primitives.

It must remain independent from any specific gameplay bounded context.

## Allowed content

- Generic result primitives
- Generic error primitives
- Time abstractions
- Technical abstractions used by multiple services

## Forbidden content

This package must not contain volatile game domain models such as:

- Run
- Room
- Node
- EnemyTemplate
- SkillTemplate
- ItemTemplate
- PalaceLawDefinition
- EventTemplate
- CombatInstance
- Reward
- PlayerProfile

## Architectural rule

Services may depend on shared technical building blocks, but they must not share their domain models.

Inter-service communication must use contracts, DTOs, snapshots, messages or events.