# Combat Encounter Drafts

## Objectif

Un `CombatEncounterDraft` est une préparation de rencontre.
Il décrit les ennemis et alliés pressentis pour une confrontation, sans résoudre le combat.

## Source de contenu

Les ennemis proviennent des `EnemyDefinitions` du Catalog Service via `ICatalogContentGateway.GetEnemyDefinitionByKeyAsync`, `ListEnemyDefinitionsByRoomTypeAsync` et `ListCompatibleEnemyDefinitionsAsync`.

## Ce que contient un draft

- contexte de run (`RunId`, `RoomId`, `NodeId`) ;
- `RoomType` ;
- `RoomIndex` (profondeur de la room) ;
- `RiskLevel` du node ;
- `EncounterType` (`Combat`, `Elite`, `Rare`, `RoomBoss`) ;
- ennemis sélectionnés (`CombatEncounterDraftEnemy`) ;
- alliés placeholders (`CombatEncounterDraftAlly`).

### CombatEncounterDraftEnemy

Champs alignés sur `CatalogEnemyDefinition` :

- `EnemyKey`
- `DisplayName`
- `Description`
- `Archetype`
- `BaseDifficulty`
- `MinRiskLevel`
- `MaxRiskLevel`
- `Tags`
- `SkillKeys`

### CombatEncounterDraftAlly

Actuellement, un seul allié placeholder est inclus :

- `player.self` / `Le Joueur` / `Protagonist`

## Ce que le draft ne contient pas

- tours ;
- dégâts ;
- HP runtime ;
- initiative ;
- IA ;
- journal de combat ;
- effets temporaires runtime ;
- `CombatantSnapshot` (état de combat).

## Déterminisme

La sélection des ennemis est **déterministe** :

1. Les ennemis compatibles sont ordonnés par `Key` (ordre alphabétique).
2. Les premiers `EnemyCount` ennemis sont sélectionnés.
3. Aucun `Random` non seedé n'est utilisé.

### Règles de sélection par `EncounterType`

| Type | Nombre d'ennemis | Règle de sélection |
|---|---|---|
| `Combat` (RiskLevel 1-2) | 1 | Premier ennemi compatible |
| `Combat` (RiskLevel 3-5) | 2 (si assez d'ennemis) | Deux premiers ennemis |
| `Elite` | 1 | Ennemi taggé `elite` ; sinon le plus difficile |
| `Rare` | 1 | Ennemi le plus difficile |
| `RoomBoss` | 1 (via `RoomBossProfile`, non implémenté dans cette PR) | N/A |

## Architecture

```
Application/Combats/EncounterDrafts/
├── CombatEncounterDraft.cs                        — Modèle principal
├── CombatEncounterDraftEnemy.cs                   — Modèle ennemi du draft
├── CombatEncounterDraftAlly.cs                    — Modèle allié du draft
├── CombatEncounterDraftContext.cs                 — Contexte de génération
└── ICombatEncounterDraftGenerator.cs              — Interface du générateur

Application/Combats/Dtos/
├── CombatEncounterDraftDto.cs                     — DTO du draft
├── CombatEncounterDraftEnemyDto.cs                — DTO ennemi
└── CombatEncounterDraftAllyDto.cs                 — DTO allié

Infrastructure/Combats/EncounterDrafts/
└── CombatEncounterDraftGenerator.cs               — Implémentation concrète

Tests/
└── Combats/EncounterDrafts/
    └── CombatEncounterDraftGeneratorTests.cs       — Tests unitaires
```

## Flow prévu

```
Node Combat / Elite / Rare
  → EventContentResolutionContext
    → IEventContentResolver.ResolveAsync()
      → CombatEncounterDraftContext
        → ICombatEncounterDraftGenerator.GenerateAsync()
          → ICatalogContentGateway.ListCompatibleEnemyDefinitionsAsync()
          → CombatEncounterDraft
```

Le draft est inclus dans la réponse `ResolveCurrentEventResponse.EncounterDraft`.

## Intégration pipeline existant

`ResolveCurrentEventCommandHandler` a été étendu pour :

1. Déterminer le type de rencontre (`Combat`, `Elite`, `Rare`, `RoomBoss`) à partir du `ResolutionKind`.
2. Calculer `EnemyCount` en fonction du type et du RiskLevel.
3. Appeler `ICombatEncounterDraftGenerator.GenerateAsync()`.
4. Inclure le DTO du draft dans la réponse.

En cas d'échec de génération du draft (ex. aucun ennemi compatible), la réponse contient `EncounterDraft = null` et le handler continue normalement (le combat `CombatInstance` est toujours créé via le pipeline existant).

## Non-objectifs

- pas de combat complet ;
- pas de gestion de tours ;
- pas de dégâts ;
- pas de `CombatService` ;
- pas d'Event Bus ;
- pas de frontend.

## Future work

- générer un vrai `CombatEncounter` runtime à partir du draft ;
- ajouter les `SkillDefinitions` ;
- intégrer les alliés réels (Neige, etc.) ;
- gérer plusieurs ennemis et plusieurs alliés ;
- ajouter initiative, tours et actions ;
- générer les drafts RoomBoss à partir des `RoomBossProfile`.
