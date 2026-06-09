# Catalog HTTP Gateway

## Objectif

Le Game Engine peut consommer les Room Boss Definitions et les Enemy Definitions exposées par le Catalog Service via `ICatalogContentGateway`.

## Modes disponibles

### InMemory (par défaut)

Mode utilisé pour le développement isolé et les tests. Aucune dépendance réseau.

### Http

Mode permettant au Game Engine d'appeler le Catalog Service pour récupérer les Room Boss Definitions et Enemy Definitions.

## Configuration

Dans `appsettings.json` ou `appsettings.Development.json` :

```json
{
  "CatalogGateway": {
    "Mode": "InMemory",
    "BaseUrl": "http://localhost:5193",
    "Timeout": "00:00:05"
  }
}
```

| Clé | Valeur par défaut | Description |
|---|---|---|
| `Mode` | `InMemory` | `InMemory` ou `Http` |
| `BaseUrl` | `http://localhost:5193` | URL du Catalog Service |
| `Timeout` | `00:00:05` | Timeout des requêtes HTTP |

## Endpoints consommés

```
GET /api/v2/catalog/room-boss-definitions/room-type/{roomType}
GET /api/v2/catalog/enemy-definitions/{key}
GET /api/v2/catalog/enemy-definitions/room-type/{roomType}
GET /api/v2/catalog/enemy-definitions/compatible?roomType={roomType}&riskLevel={riskLevel}
```

## Règles

### Room Boss Profiles

| Réponse Catalog | Comportement |
|---|---|
| `200 OK` avec `definition` non-null | Retourne `CatalogRoomBossProfile` mappé |
| `200 OK` avec `definition` null | Retourne `null` |
| `400 Bad Request` | Retourne `null` |
| `404 Not Found` | Retourne `null` |
| `5xx` ou erreur réseau | Lève `CatalogGatewayException` |

### Enemy Definitions

| Méthode | Réponse Catalog | Comportement |
|---|---|---|
| `GetEnemyDefinitionByKeyAsync` | `200 OK` avec `definition` non-null | Retourne `CatalogEnemyDefinition` mappé |
| `GetEnemyDefinitionByKeyAsync` | `200 OK` avec `definition` null | Retourne `null` |
| `GetEnemyDefinitionByKeyAsync` | `400 Bad Request` | Retourne `null` |
| `GetEnemyDefinitionByKeyAsync` | `404 Not Found` | Retourne `null` |
| `GetEnemyDefinitionByKeyAsync` | `5xx` ou erreur réseau | Lève `CatalogGatewayException` |
| `ListEnemyDefinitionsByRoomTypeAsync` / `ListCompatibleEnemyDefinitionsAsync` | `200 OK` | Retourne la liste mappée |
| `ListEnemyDefinitionsByRoomTypeAsync` / `ListCompatibleEnemyDefinitionsAsync` | `400 Bad Request` | Retourne liste vide |
| `ListEnemyDefinitionsByRoomTypeAsync` / `ListCompatibleEnemyDefinitionsAsync` | `404 Not Found` | Retourne liste vide |
| `ListEnemyDefinitionsByRoomTypeAsync` / `ListCompatibleEnemyDefinitionsAsync` | `5xx` ou erreur réseau | Lève `CatalogGatewayException` |

- Aucun fallback silencieux vers InMemory en mode Http.
- Le `CancellationToken` est propagé à l'appel HTTP.
- Les methods non encore disponibles lèvent `CatalogGatewayException`.

## Limitation actuelle

Le mode HTTP couvre actuellement :

- `GetRoomBossProfileAsync`
- `GetEnemyDefinitionByKeyAsync`
- `ListEnemyDefinitionsByRoomTypeAsync`
- `ListCompatibleEnemyDefinitionsAsync`

Les contenus suivants restent disponibles uniquement via l'implémentation InMemory :

- Event templates
- Enemy templates (combat-ready with stats)
- Item templates
- Palace law definitions
- Skill templates

En conséquence, `CatalogGateway:Mode = Http` permet de valider l'intégration Game Engine ↔ Catalog pour les boss de Room et les Enemy Definitions, mais ne permet pas encore d'exécuter toute la boucle jouable jusqu'à la résolution complète des events.

Pour le flow jouable complet, utiliser :

```json
{
  "CatalogGateway": {
    "Mode": "InMemory"
  }
}
```

Cette limitation est volontaire et temporaire.

Les Enemy Definitions restent des définitions de contenu. Elles ne créent pas encore de `CombatEncounter` et ne représentent pas un état de combat runtime.

## Architecture

```
Game Engine Application
├── Catalog/
│   ├── CatalogRoomBossProfile.cs                         — DTO Room Boss applicatif
│   ├── CatalogEnemyDefinition.cs                         — DTO Enemy Definition applicatif
│   ├── InMemoryCatalogContentGateway.cs                  — Implémentation InMemory
│   └── Ports/
│       └── ICatalogContentGateway.cs                     — Port applicatif

Game Engine Infrastructure
├── Catalog/
│   ├── CatalogGatewayOptions.cs                          — Options strongly typed
│   ├── CatalogRoomBossDefinitionHttpResponse.cs          — DTO HTTP interne RoomBoss
│   ├── CatalogEnemyDefinitionHttpResponse.cs             — DTO HTTP interne Enemy
│   ├── CatalogGatewayException.cs                        — Exception dédiée
│   └── HttpCatalogContentGateway.cs                      — Implémentation HTTP du port ICatalogContentGateway
└── DependencyInjection/
    └── InfrastructureServiceCollectionExtensions.cs      — DI : InMemory ou Http selon Mode

Tests
└── Catalog/
    ├── HttpCatalogContentGatewayTests.cs                 — Tests unitaires (fake HttpMessageHandler)
    └── InMemoryCatalogContentGatewayTests.cs             — Tests unitaires InMemory
```

## Non-objectifs

- pas de RabbitMQ ;
- pas d'Event Bus ;
- pas de retry policy avancée ;
- pas de circuit breaker ;
- pas de cache Redis ;
- pas de combat ;
- pas de service discovery.

## Future work

- Exposer les endpoints Catalog pour Skill, Item, Event, Palace Law templates
- Ajouter une politique de résilience
- Remplacer progressivement les données InMemory par des appels HTTP
