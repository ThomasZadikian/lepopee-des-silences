# alpha-0.8.1 — Définitions PNJ dans Catalog Service

## Objectif

Corriger l'architecture des définitions PNJ en les déplaçant du Game Engine vers le Catalog Service, où elles constituent la source de vérité unique pour le contenu stable des PNJ.

## Problème corrigé

Initialement, les 5 PNJ de base (`npc-neutral-traveler`, `npc-silent-witness`, `npc-desert-exile`, `npc-rage-beggar`, `npc-rain-memory-keeper`) étaient définis directement dans `InMemoryCatalogContentGateway` côté Game Engine, sans endpoint Catalog ni persistance.

**Problèmes :**
- Les définitions PNJ sont du contenu stable qui doit appartenir au Catalog Service
- Le Game Engine ne doit pas être la source de vérité pour les définitions de contenu
- Aucun endpoint HTTP ne permettait de servir les PNJ aux consommateurs Catalog
- La PR d'éligibilité adaptative PNJ partait sur une base architecturale incorrecte

## Pourquoi les PNJ appartiennent au Catalog Service

- Les PNJ ont des caractéristiques stables (nom, description, tags, compatibilités)
- Ils suivent le même cycle de vie que les autres définitions du Catalog (EnemyDefinition, SkillDefinition, PalaceLawDefinition)
- Le Catalog Service expose les définitions via l'API HTTP, que le Game Engine consomme via `ICatalogContentGateway`
- Le Game Engine ne doit pas définir ni posséder de contenu stable — il le consomme

## Contrat consommé par Game Engine

Le Game Engine consomme `CatalogNpcDefinition` via `ICatalogContentGateway.ListNpcDefinitionsAsync()` :

```csharp
public sealed record CatalogNpcDefinition(
    string Key,
    string DisplayName,
    string Description,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> CompatibleRoomTypes,
    IReadOnlyCollection<PalaceRoomState> CompatiblePalaceRoomStates,
    IReadOnlyCollection<string> CompatibleRoomClimates,
    int MinDepth,
    int MaxDepth);
```

L'implémentation HTTP (`HttpCatalogContentGateway`) appelle `GET /api/v2/catalog/npc-definitions` et mappe la réponse DTO vers le record Game Engine, en convertissant les `PalaceRoomState` (strings dans Catalog) vers l'enum Game Engine.

## Rôle du InMemoryCatalogContentGateway

Les 5 PNJ sont conservés dans `InMemoryCatalogContentGateway` comme **fallback dev/test uniquement**.

Ce rôle est explicitement documenté dans le code :
- Commentaire clair sur le dictionnaire `NpcDefinitions`
- Uniquement pour les environnements locaux où `Persistence:Mode != Postgres`
- En production/staging, l'`HttpCatalogContentGateway` interroge le Catalog API

Cette approche est cohérente avec les autres définitions du InMemory (ennemis, skills, etc.).

## Schéma Catalog

### Table `catalog_npc_definitions`

| Colonne | Type | Description |
|---------|------|-------------|
| id | guid | PK |
| key | varchar(160) | Identifiant unique |
| name | varchar(256) | Nom court |
| display_name | varchar(256) | Nom d'affichage |
| description | text | Description |
| version | varchar(64) | Version |
| status | varchar(32) | Draft / Active / Deprecated / Disabled |
| min_depth | int? | Profondeur minimale |
| max_depth | int? | Profondeur maximale |
| compatible_room_types_json | text | Types de salle compatibles (JSON) |
| compatible_palace_room_states_json | text | États de salle compatibles (JSON) |
| compatible_room_climates_json | text | Climats compatibles (JSON) |
| tags_json | text | Tags (JSON) |

### Endpoint API

```
GET /api/v2/catalog/npc-definitions
→ 200 ListActiveNpcDefinitionsResponse { Definitions: NpcDefinitionDto[] }
```

Le `NpcDefinitionDto` expose les champs suivants :
- `Id`, `Key`, `Name`, `Description`, `Version`, `Status`
- `Tags`, `CompatibleRoomTypes`, `CompatiblePalaceRoomStates`, `CompatibleRoomClimates`
- `MinDepth`, `MaxDepth`

Les `CompatiblePalaceRoomStates` et `CompatibleRoomClimates` sont des `string[]` dans Catalog, car ces types sont des concepts Game Engine. Le mapping vers les enums Game Engine se fait dans `HttpCatalogContentGateway`.

## Champs PNJ ajoutés

| Champ | Catalog Type | Game Engine Type | Description |
|-------|-------------|-----------------|-------------|
| CompatibleRoomTypes | string[] | string[] | Types de salle où le PNJ peut apparaître |
| CompatiblePalaceRoomStates | string[] | PalaceRoomState[] | États de Palais compatibles |
| CompatibleRoomClimates | string[] | string[] | Climats compatibles |
| MinDepth | int? → int | int (default 0) | Profondeur minimale d'apparition |
| MaxDepth | int? → int | int (default MaxValue) | Profondeur maximale d'apparition |

## PNJ seedés (Catalog + InMemory fallback)

Les mêmes 5 PNJ côté Catalog (statut Active) :

| Key | Contrainte |
|-----|-----------|
| npc-neutral-traveler | Aucune — fallback générique |
| npc-silent-witness | PalaceRoomState: Silent |
| npc-desert-exile | RoomClimate: Heatwave |
| npc-rage-beggar | PalaceRoomState: Enraged |
| npc-rain-memory-keeper | RoomClimate: Rain |

## Tests

### Catalog Service (239 tests, 0 échec)

Domain :
- `NpcDefinitionTests` : création valide, contraintes état/climat, déduplication tags, statut Active

Application :
- `NpcDefinitionQueryHandlerTests` : ListActive → definitions mappées, vide si aucune, état/climat mappés

Infrastructure :
- `InMemoryNpcDefinitionReadStoreTests` : 5 seeds actifs, contraintes correctes par PNJ, fallback neutre sans contrainte

### Game Engine Gateway (1054 tests, 0 échec, 16 ignorés)

`HttpCatalogContentGatewayTests` — nouveaux tests :
- ListNpcDefinitionsAsync retourne les PNJ
- Mapping des PalaceRoomStates (Silent / Enraged)
- Mapping des RoomClimates (Heatwave / Rain)
- Mapping des MinDepth/MaxDepth (avec valeurs null → defaults)
- 404 → vide ; 400 → vide ; endpoint correct ; 500 → exception

### Confidentialité vérifiée

Aucune réponse Catalog ou Game Engine ne contient : weight, probability, matrix, markov, rawScore, adaptiveScore.

## Ce qui n'est pas encore implémenté (cette PR)

- Sélection PNJ par contexte adaptatif de salle
- Choix/dialogues PNJ dynamiques
- Rewards PNJ
- Relations joueur/PNJ
- Narration adaptative
- Frontend PNJ
- Palace Pressure complet
- Migration EF Catalog (la table est créée avec `EnsureCreated` pour le moment)

## Points reportés (PR future : adaptive room context npc eligibility)

- `NpcEligibilityContext` à créer
- `NpcEncounterSelector` à implémenter
- Branchement dans `NpcNodeEventResolver`
- Tests d'éligibilité par état/climat

## Fichiers créés ou modifiés

### Catalog Service (créés)
- `Domain/Npcs/INpcDefinition.cs`
- `Domain/Npcs/NpcDefinition.cs`
- `Application/Npcs/Definitions/Ports/INpcDefinitionReadStore.cs`
- `Application/Npcs/Definitions/Dtos/NpcDefinitionDto.cs`
- `Application/Npcs/Definitions/ListActiveNpcDefinitions/*` (Query, Handler, Response)
- `Infrastructure/Persistence/Entities/NpcDefinitionEntity.cs`
- `Infrastructure/Persistence/Configurations/NpcDefinitionEntityConfiguration.cs`
- `Infrastructure/ReadStores/InMemoryNpcDefinitionReadStore.cs`
- `Infrastructure/ReadStores/Ef/EfNpcDefinitionReadStore.cs`
- `Api/Controllers/NpcDefinitionsController.cs`

### Catalog Service (modifiés)
- `Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs`
- `Infrastructure/Persistence/CatalogDbContext.cs`

### Tests Catalog (créés)
- `Domain/Npcs/NpcDefinitionTests.cs`
- `Application/Npcs/NpcDefinitionQueryHandlerTests.cs`
- `Infrastructure/ReadStores/InMemoryNpcDefinitionReadStoreTests.cs`

### Game Engine (créés)
- *(aucun nouveau fichier)*

### Game Engine (modifiés)
- `HttpCatalogContentGateway.cs` : implémentation HTTP de `ListNpcDefinitionsAsync` + DTO + mapping
- `HttpCatalogContentGatewayTests.cs` : 10 nouveaux tests

## Validations exécutées

```
dotnet build services/catalog/Leds.Catalog.slnx    → 0 erreur
dotnet test services/catalog/Leds.Catalog.UnitTests → 239 réussis, 0 échec

dotnet build services/game-engine/Leds.GameEngine.slnx → 0 erreur
dotnet test services/game-engine/Leds.GameEngine.UnitTests → 1054 réussis, 0 échec, 16 ignorés
```

Aucun nouveau `Skip` ajouté.
