# Enemy Definitions

## Objectif

Les Enemy Definitions décrivent les ennemis disponibles dans le Catalog Service.

## Responsabilité

Le Catalog Service est la source de vérité du contenu.
Le Game Engine consommera ces définitions pour préparer les futures rencontres de combat.

## Ce qu'est une Enemy Definition

Une Enemy Definition décrit :

- l'identité stable de l'ennemi ;
- son nom ;
- sa description ;
- son archétype (Fragile, Guard, Bruiser, Support, Skirmisher, Disruptor, Elite) ;
- les RoomTypes compatibles ;
- sa difficulté de base ;
- sa plage de RiskLevel ;
- ses tags ;
- ses SkillKeys potentielles.

## Ce que ce n'est pas

Une Enemy Definition n'est pas un état de combat runtime.

Elle ne contient pas :

- CurrentHp ;
- initiative actuelle ;
- cible actuelle ;
- état de tour ;
- dégâts calculés ;
- effets temporaires runtime.

## Endpoints

Tous les endpoints sont sous `api/v2/catalog/enemy-definitions`.

### GET /api/v2/catalog/enemy-definitions

Retourne tous les ennemis actifs.

### GET /api/v2/catalog/enemy-definitions/{key}

Retourne un ennemi par sa clé.

| Status | Condition |
|---|---|
| 200 OK | Définition trouvée |
| 400 Bad Request | Clé vide/blanche/trop longue |
| 404 Not Found | Aucune définition trouvée |

### GET /api/v2/catalog/enemy-definitions/room-type/{roomType}

Retourne les ennemis compatibles avec un RoomType.

| Status | Condition |
|---|---|
| 200 OK | Toujours (liste possiblement vide) |
| 400 Bad Request | RoomType vide/blanc/trop long |

### GET /api/v2/catalog/enemy-definitions/compatible?roomType={roomType}&riskLevel={riskLevel}

Retourne les ennemis compatibles avec un RoomType et un RiskLevel.

Filtre :
- `CompatibleRoomTypes` contient `roomType`
- `MinRiskLevel <= riskLevel <= MaxRiskLevel`

| Status | Condition |
|---|---|
| 200 OK | Toujours (liste possiblement vide) |
| 400 Bad Request | RoomType vide ou riskLevel hors plage 1-5 |

## Seed data

| Key | DisplayName | Archetype | RoomTypes | Risk | Difficulty |
|---|---|---|---|---|---|
| `enemy.threshold.doubt-fragment` | Fragment de Doute | Fragile | Threshold | 1-2 | 1 |
| `enemy.threshold.inner-resistance` | Résistance Intérieure | Guard | Threshold | 2-3 | 2 |
| `enemy.forest.rooted-regret` | Regret Enraciné | Bruiser | Forest | 1-3 | 2 |
| `enemy.forest.whispering-branch` | Branche Murmurante | Support | Forest | 2-4 | 2 |
| `enemy.rupture.broken-thought` | Pensée Brisée | Skirmisher | Rupture | 2-4 | 3 |
| `enemy.rupture.contradiction` | Contradiction | Disruptor | Rupture | 3-5 | 4 |
| `enemy.silence.mute-witness` | Témoin Muet | Guard | Silence | 2-4 | 3 |
| `enemy.silence.absent-voice` | Voix Absente | Disruptor | Silence | 3-5 | 4 |
| `enemy.memory.archived-wound` | Blessure Archivée | Bruiser | Memory | 2-5 | 4 |
| `enemy.memory.named-loss` | Perte Nommée | Support | Memory | 3-5 | 4 |
| `enemy.antechamber.door-keeper` | Gardien de Porte | Guard | Antechamber | 3-5 | 5 |
| `enemy.antechamber.last-refusal` | Dernier Refus | Bruiser | Antechamber | 4-5 | 5 |
| `enemy.final.silent-double` | Double Silencieux | Elite | Final | 4-5 | 8 |
| `enemy.final.last-echo` | Dernier Écho | Elite | Final | 4-5 | 9 |

## Architecture

```
Domain/Enemies/
├── IEnemyDefinition.cs           — Interface domaine
└── EnemyDefinition.cs            — Entité (extends CatalogContentBase)

Application/Enemies/Definitions/
├── Dtos/
│   └── EnemyDefinitionDto.cs     — DTO response avec FromDomain()
├── Ports/
│   └── IEnemyDefinitionReadStore.cs   — Contrat read store
├── ListActiveEnemyDefinitions/        — Query, Handler, Response
├── GetEnemyDefinitionByKey/           — Query, Handler, Response, Validator
├── ListEnemyDefinitionsByRoomType/    — Query, Handler, Response, Validator
└── ListCompatibleEnemyDefinitions/    — Query, Handler, Response, Validator

Infrastructure/ReadStores/
└── InMemoryEnemyDefinitionReadStore.cs   — 14 seeds

Api/Controllers/
└── EnemyDefinitionsController.cs    — REST endpoints

Tests (UnitTests)
├── Application/Enemies/Definitions/
│   ├── EnemyDefinitionTests.cs
│   ├── EnemyDefinitionQueryHandlerTests.cs
│   ├── GetEnemyDefinitionByKeyQueryValidatorTests.cs
│   ├── ListEnemyDefinitionsByRoomTypeQueryValidatorTests.cs
│   └── ListCompatibleEnemyDefinitionsQueryValidatorTests.cs
└── Infrastructure/ReadStores/
    └── InMemoryEnemyDefinitionReadStoreTests.cs

Tests (IntegrationTests)
└── Enemies/Definitions/
    └── EnemyDefinitionEndpointTests.cs
```

## Future work

- exposer les Skill Definitions ;
- permettre au Game Engine de consommer les Enemy Definitions ;
- générer des CombatEncounterDrafts ;
- préparer le combat multi-alliés / multi-ennemis.

## Non-objectifs

- pas de combat ;
- pas de gestion de tours ;
- pas de HP runtime ;
- pas d'IA ;
- pas de loot table ;
- pas de RabbitMQ ;
- pas d'Event Bus.
