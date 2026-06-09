# Room Boss Definitions

## Overview

Room Boss Definitions represent the boss encounters within the Memory Palace's rooms. Each room type in the palace has a corresponding boss definition with a difficulty rating, thematic tags, and associated metadata.

## Domain Model

```
RoomBossDefinition : CatalogContentBase
- Id: CatalogContentId (Guid)
- Key: CatalogContentKey (string) — stable key, e.g. "boss.threshold.warden"
- Name: CatalogContentName (string)
- Description: CatalogContentDescription (string)
- Version: CatalogContentVersion (string)
- Status: CatalogContentStatus (Draft | Active | Deprecated | Disabled)
- RoomType: string — e.g. "Threshold", "Forest", "Rupture", "Silence", "Antechamber", "Memory", "Final"
- BaseDifficulty: int (1-10) — baseline difficulty, not yet linked to combat
- Tags: IReadOnlyCollection<string> — unique tags deduplicated by OrdinalIgnoreCase

```

## Key Design Decisions

- **Key stability**: Catalog keys (e.g. `boss.threshold.warden`) never include a `-v1` suffix. The versioned template key is managed by the Game Engine at runtime.
- **No combat details**: This PR intentionally excludes HP, skills, AI behavior, loot tables, and reward configuration. Those will be introduced in future PRs linking to `EnemyTemplate` and combat mechanics.
- **No `EnemyTemplateKey`**: The catalog-level DTO does not expose an `EnemyTemplateKey` because that is a runtime template identifier belonging to the Game Engine.
- **`BaseDifficulty` is a standalone integer**: It is not yet wired into any combat calculation. It serves as the foundational difficulty seed for future combat balancing.

## API Endpoints

All endpoints are under `api/v2/catalog/room-boss-definitions`.

### List all active definitions
```
GET /api/v2/catalog/room-boss-definitions
```
Returns all active room boss definitions.

**Response `200 OK`**:
```json
{
  "definitions": [
    {
      "id": "guid",
      "key": "boss.threshold.warden",
      "name": "Warden of the Threshold",
      "description": "...",
      "version": "1.0.0",
      "status": "Active",
      "roomType": "Threshold",
      "baseDifficulty": 1,
      "tags": ["sentinel", "guardian", "threshold"]
    }
  ]
}
```

### Get by key
```
GET /api/v2/catalog/room-boss-definitions/{key}
```
Returns a single definition matching the given key.

| Status | Condition |
|---|---|
| 200 OK | Definition found |
| 400 Bad Request | Key is empty/whitespace/too long (FluentValidation) |
| 404 Not Found | No definition matches the key |

### Get by room type
```
GET /api/v2/catalog/room-boss-definitions/room-type/{roomType}
```
Returns a single definition matching the given room type.

| Status | Condition |
|---|---|
| 200 OK | Definition found |
| 400 Bad Request | Room type is empty/whitespace/too long (FluentValidation) |
| 404 Not Found | No definition matches the room type |

## Seed Data

| Key | Name | Room Type | BaseDifficulty | Tags |
|---|---|---|---|---|
| `boss.threshold.warden` | Warden of the Threshold | Threshold | 1 | sentinel, guardian, threshold |
| `boss.forest.rootbound-memory` | Rootbound Memory | Forest | 2 | ancient, forest, roots |
| `boss.rupture.fractured-echo` | Fractured Echo | Rupture | 3 | shattered, echo, rupture |
| `boss.silence.mute-herald` | Mute Herald | Silence | 4 | silent, herald, void |
| `boss.antechamber.last-door` | The Last Door | Antechamber | 5 | barrier, ward, antechamber |
| `boss.memory.archivist` | Archivist of Lost Moments | Memory | 4 | archivist, memory, keeper |
| `boss.final.himlit` | Himlit | Final | 10 | final, eldritch, heart |

## Architecture

```
Domain/RoomBosses/
├── IRoomBossDefinition.cs          — Domain interface
└── RoomBossDefinition.cs           — Entity (extends CatalogContentBase)

Application/RoomBosses/
├── Dtos/
│   └── RoomBossDefinitionDto.cs    — Response DTO with FromDomain()
├── Ports/
│   └── IRoomBossDefinitionReadStore.cs  — Read store contract
├── ListActiveRoomBossDefinitions/       — Query, Handler, Response
├── GetRoomBossDefinitionByKey/          — Query, Handler, Response, Validator
└── GetRoomBossDefinitionByRoomType/     — Query, Handler, Response, Validator

Infrastructure/ReadStores/
└── InMemoryRoomBossDefinitionReadStore.cs  — In-memory implementation with 7 seeds

Api/Controllers/
└── RoomBossDefinitionsController.cs    — REST endpoints

Tests (UnitTests)
├── Application/RoomBosses/
│   ├── RoomBossDefinitionTests.cs                  — Domain creation logic
│   ├── RoomBossDefinitionQueryHandlerTests.cs      — Handler mapping
│   ├── GetRoomBossDefinitionByKeyQueryValidatorTests.cs
│   └── GetRoomBossDefinitionByRoomTypeQueryValidatorTests.cs
└── Infrastructure/ReadStores/
    └── InMemoryRoomBossDefinitionReadStoreTests.cs — Read store queries

Tests (IntegrationTests)
└── RoomBosses/
    └── RoomBossDefinitionEndpointTests.cs           — HTTP endpoint tests
```

## Future Work

- Link `RoomBossDefinition` to `EnemyTemplate` for combat mechanics
- Add loot/reward configuration per boss
- Add HP, skills, and AI behavior definitions
- Move read store from InMemory to a persistent database
