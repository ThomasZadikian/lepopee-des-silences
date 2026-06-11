# Player Service PostgreSQL Persistence

## Objectif

Persister les profils joueurs, la progression permanente et l'idempotence des événements via PostgreSQL.

## Tables

| Table | Description |
|---|---|
| `player_profiles` | Profil joueur (displayName, progression counters) |
| `player_characters` | Personnages du joueur (definitionKey, stats, skills JSON) |
| `player_processed_integration_events` | Idempotence des événements Game Engine → Player |

## Architecture

```
PlayerProfile (aggregate root)
├── PlayerRoster (owned collection)
│   └── PlayerCharacter[]
├── PlayerProgression (owned value object)
│   ├── TotalRunsStarted
│   ├── TotalRunsCompleted
│   ├── TotalRunsFailed
│   └── TotalRunsAbandoned
└── ProcessedIntegrationEvent (separate table)
```

## Configuration

### InMemory (défaut pour tests/dev)

```json
{
  "Persistence": {
    "Mode": "InMemory"
  }
}
```

### PostgreSQL

```json
{
  "Persistence": {
    "Mode": "Postgres"
  },
  "ConnectionStrings": {
    "PlayerDb": "Host=localhost;Port=5433;Database=leds_player;Username=postgres;Password=postgres"
  }
}
```

## Commandes EF Core

### Créer une migration

```bash
dotnet ef migrations add <Name> \
  --project src/Leds.Player.Infrastructure \
  --startup-project src/Leds.Player.Api \
  --context PlayerDbContext \
  --output-dir Persistence/Migrations
```

### Appliquer les migrations

```bash
dotnet ef database update \
  --project src/Leds.Player.Infrastructure \
  --startup-project src/Leds.Player.Api \
  --context PlayerDbContext
```

## Fichiers importants

| Fichier | Rôle |
|---|---|
| `Infrastructure/Persistence/PlayerDbContext.cs` | DbContext EF Core |
| `Infrastructure/Persistence/PlayerDbContextFactory.cs` | Design-time factory |
| `Infrastructure/Persistence/Entities/PlayerProfileEntity.cs` | Entity profil |
| `Infrastructure/Persistence/Entities/PlayerCharacterEntity.cs` | Entity personnage |
| `Infrastructure/Persistence/Entities/ProcessedIntegrationEventEntity.cs` | Entity idempotence |
| `Infrastructure/Persistence/Repositories/EfPlayerProfileRepository.cs` | Repository profil |
| `Infrastructure/Persistence/Repositories/EfProcessedIntegrationEventRepository.cs` | Repository idempotence |

## Non-objectifs

- pas d'Identity
- pas d'historique complet de run
- pas de dashboard Player
- pas de vraie authentification
