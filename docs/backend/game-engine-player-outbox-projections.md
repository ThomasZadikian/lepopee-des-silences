# Game Engine to Player Outbox Projections

## Objectif

Documenter le flow de projection des résultats de run depuis Game Engine vers Player Service via le pattern Outbox.

## Architecture

```
Game Engine (transaction locale)
├── État métier mis à jour (Run, Combat, etc.)
├── Outbox message écrit dans game_engine_outbox_messages
└── Transaction commitée (single SaveChangesAsync)

Game Engine Outbox Dispatcher (in-process, polling)
├── Lit les messages non traités
├── Appelle Player Service endpoint interne
├── Marque le message comme processed
└── Gère les retries

Player Service (idempotent)
├── Reçoit l'événement
├── Vérifie si déjà traité (eventId)
├── Met à jour PlayerProgression
└── Marque eventId comme processed
```

## Événements supportés

| Événement | Description | Effet Player |
|---|---|---|
| `RunCompletedIntegrationEvent` | Run terminée avec succès | `TotalRunsCompleted += 1` |
| `RunFailedIntegrationEvent` | Run échouée | `TotalRunsFailed += 1` |
| `RunAbandonedIntegrationEvent` | Run abandonnée | `TotalRunsAbandoned += 1` |

## Tables Game Engine

### game_engine_outbox_messages

| Colonne | Type | Description |
|---|---|---|
| id | UUID | PK |
| type | VARCHAR(128) | Type d'événement |
| event_version | VARCHAR(32) | Version du payload |
| payload_json | TEXT | Payload sérialisé |
| occurred_at_utc | TIMESTAMP | Date de l'événement métier |
| created_at_utc | TIMESTAMP | Date d'insertion |
| processed_at_utc | TIMESTAMP? | Date de traitement réussi |
| retry_count | INT | Nombre de tentatives |
| last_error | TEXT? | Dernière erreur |
| correlation_id | UUID? | Corrélation |
| causation_id | UUID? | Causalité |
| destination | VARCHAR(128)? | Destination cible |

## Configuration

```json
{
  "Outbox": {
    "DispatcherEnabled": true,
    "PollingIntervalSeconds": 10,
    "BatchSize": 20,
    "MaxRetryCount": 10
  }
}
```

## Fichiers importants

### Game Engine

- `Application/IntegrationEvents/RunIntegrationEvents.cs`
- `Infrastructure/Persistence/Entities/OutboxMessageEntity.cs`
- `Infrastructure/Persistence/Outbox/EfOutboxWriter.cs`
- `Infrastructure/Outbox/GameEngineOutboxDispatcherHostedService.cs`
- `Infrastructure/Outbox/HttpPlayerProjectionClient.cs`

### Player

- `Application/Internal/ConsumeRunOutcome/ConsumeRunOutcomeCommand.cs`
- `Application/Internal/ConsumeRunOutcome/ConsumeRunOutcomeCommandHandler.cs`
- `Api/Controllers/InternalProjectionsController.cs`
- `Application/Abstractions/IProcessedIntegrationEventRepository.cs`
- `Infrastructure/Persistence/InMemory/InMemoryProcessedIntegrationEventRepository.cs`

## Limites actuelles

- Dispatcher in-process (pas RabbitMQ)
- Pas d'auth service-to-service
- Player Service InMemory seulement (pas PostgreSQL)
- Pas de métriques/observabilité sur le dispatcher
- Pas de dashboard Player
