# Catalog HTTP Gateway

## Objectif

Le Game Engine peut consommer les Room Boss Definitions exposées par le Catalog Service via `ICatalogContentGateway`.

## Modes disponibles

### InMemory (par défaut)

Mode utilisé pour le développement isolé et les tests. Aucune dépendance réseau.

### Http

Mode permettant au Game Engine d'appeler le Catalog Service pour récupérer les Room Boss Definitions.

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

## Endpoint consommé

```
GET /api/v2/catalog/room-boss-definitions/room-type/{roomType}
```

## Règles

| Réponse Catalog | Comportement |
|---|---|
| `200 OK` avec `definition` non-null | Retourne `CatalogRoomBossProfile` mappé |
| `200 OK` avec `definition` null | Retourne `null` |
| `400 Bad Request` | Retourne `null` |
| `404 Not Found` | Retourne `null` |
| `5xx` ou erreur réseau | Lève `CatalogGatewayException` |

- Aucun fallback silencieux vers InMemory en mode Http.
- Le `CancellationToken` est propagé à l'appel HTTP.
- Les autres méthodes du gateway (`GetEnemyTemplateByKeyAsync`, etc.) lèvent `CatalogGatewayException` car ces endpoints Catalog ne sont pas encore exposés.

## Limitation actuelle

Le mode HTTP ne couvre actuellement que les Room Boss Definitions.

La méthode suivante est disponible via HTTP :

- `GetRoomBossProfileAsync`

Les autres contenus restent disponibles uniquement via l'implémentation InMemory :

- Event templates
- Enemy templates
- Item templates
- Palace law definitions
- Skill templates

En conséquence, `CatalogGateway:Mode = Http` permet de valider l'intégration Game Engine ↔ Catalog pour la génération des boss de Room, mais ne permet pas encore d'exécuter toute la boucle jouable jusqu'à la résolution complète des events.

Pour le flow jouable complet, utiliser :

```json
{
  "CatalogGateway": {
    "Mode": "InMemory"
  }
}
```

Cette limitation est volontaire et temporaire. Les prochains travaux ajouteront progressivement les définitions manquantes au Catalog Service, en commençant par les Enemy Definitions.

## Architecture

```
Game Engine Infrastructure
├── Catalog/
│   ├── CatalogGatewayOptions.cs                          — Options strongly typed
│   ├── CatalogRoomBossDefinitionHttpResponse.cs          — DTO HTTP interne
│   ├── CatalogGatewayException.cs                        — Exception dédiée
│   └── HttpCatalogContentGateway.cs                      — Implémentation HTTP du port ICatalogContentGateway
└── DependencyInjection/
    └── InfrastructureServiceCollectionExtensions.cs      — DI : InMemory ou Http selon Mode

Tests
└── Catalog/
    └── HttpCatalogContentGatewayTests.cs                 — Tests unitaires (fake HttpMessageHandler)
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

- Exposer les endpoints Catalog pour Enemy, Skill, Item, Event, Palace Law templates
- Ajouter une politique de résilience
- Remplacer progressivement les données InMemory par des appels HTTP
