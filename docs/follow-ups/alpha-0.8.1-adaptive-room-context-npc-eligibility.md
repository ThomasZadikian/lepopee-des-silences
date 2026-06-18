# alpha-0.8.1 — Éligibilité PNJ adaptative par contexte de salle

## Objectif

Rendre la sélection des PNJ contextuelle : adapter l'apparition des PNJ dans les salles en fonction du `PalaceRoomState` et du `RoomClimate` (et à terme du type de salle et de la profondeur).

## Problème résolu

Le `NpcNodeEventResolver` retournait un texte fixe pour toute rencontre PNJ, sans consulter le Catalog ni le contexte de la salle. Les définitions PNJ (ajoutées dans la PR précédente `catalog-npc-definitions`) étaient disponibles dans le Catalog Service mais jamais utilisées par le resolver.

## Architecture

Deux pipelines existent pour la résolution d'événements :

1. **Pipeline synchrone** (`INodeEventResolver` / `NpcNodeEventResolver`) : produit le texte d'affichage (titre, description, choix)
2. **Pipeline asynchrone** (`IEventContentResolutionStrategy` / `NpcEventContentResolutionStrategy`) : résout le contenu effectif via Catalog (ajouté dans cette PR pour les événements PNJ)

Le `NpcEventContentResolutionStrategy` utilise désormais `ICatalogContentGateway` + `INpcEncounterSelector` pour sélectionner le PNJ éligible. Le `ResolveCurrentEventCommandHandler` applique ensuite le résultat au texte d'affichage (comme pour les événements PalaceLaw).

## Nouvelles types

### `NpcEligibilityContext` (Application)

```csharp
public sealed record NpcEligibilityContext(
    Guid RunId,
    Guid RoomId,
    Guid NodeId,
    string Seed,
    PalaceRoomState PalaceRoomState,
    string? RoomClimate,
    RoomType RoomType,
    int NodeDepth);
```

### `INpcEncounterSelector` (Application)

```csharp
public interface INpcEncounterSelector
{
    CatalogNpcDefinition? SelectEligibleNpc(
        NpcEligibilityContext context,
        IReadOnlyCollection<CatalogNpcDefinition> allNpcs);
}
```

### `NpcEncounterSelector` (Application)

Logique de sélection déterministe :

1. Filtre par profondeur (`MinDepth` / `MaxDepth`) et type de salle (`CompatibleRoomTypes`)
2. Cherche les PNJ **avec contraintes** (`CompatiblePalaceRoomStates` > 0 ou `CompatibleRoomClimates` > 0 ou `CompatibleRoomTypes` > 0) qui matchent le contexte courant
3. Si au moins un match spécifique → sélection déterministe (seed + runId + roomId + nodeId)
4. Si aucun match spécifique → fallback vers les PNJ **sans contrainte** (ex: `npc-neutral-traveler`)
5. Si aucun PNJ éligible → `null` (le resolver affiche un texte générique)

## Modifications apportées

### Fichiers créés

- `src/Leds.GameEngine.Application/Events/Npcs/NpcEligibilityContext.cs`
- `src/Leds.GameEngine.Application/Events/Npcs/INpcEncounterSelector.cs`
- `src/Leds.GameEngine.Application/Events/Npcs/NpcEncounterSelector.cs`

### Fichiers modifiés

- `EventContentResolutionContext.cs` : ajout de `PalaceRoomState?` et `RoomClimate?`
- `ResolvedNpcEventContent.cs` : ajout de `NpcDisplayName` et `NpcDescription` (optionnels)
- `NpcEventContentResolutionStrategy.cs` : utilise `ICatalogContentGateway` + `INpcEncounterSelector` pour sélectionner le PNJ
- `ResolveCurrentEventCommandHandler.cs` : traite `NodeEventType.Npc` dans le pipeline asynchrone (comme Law) ; applique `ApplyNpcContent` ; passe `PalaceRoomState` et `RoomClimate` au contexte
- `ApplicationServiceCollectionExtensions.cs` : enregistre `INpcEncounterSelector` / `NpcEncounterSelector` en singleton

### Tests

- `tests/Leds.GameEngine.UnitTests/Events/Npcs/NpcEncounterSelectorTests.cs` : 13 tests
  - Retourne `null` si aucun PNJ
  - PNJ neutre sélectionné par défaut (aucune contrainte)
  - PNJ spécifique sélectionné par état (`Silent` → `npc-silent-witness`, `Enraged` → `npc-rage-beggar`)
  - PNJ spécifique sélectionné par climat (`Heatwave` → `npc-desert-exile`, `Rain` → `npc-rain-memory-keeper`)
  - Fallback vers neutre si aucun match spécifique
  - Déterminisme (même seed → même résultat)
  - Respect de `MinDepth` / `MaxDepth`
  - Respect de `CompatibleRoomTypes`
  - Priorité du match spécifique sur le fallback
  - Retourne `null` quand tous les PNJ éligibles sont filtrés par contrainte

## Seed PNJ et comportement attendu

| PNJ | Contrainte | Contexte d'apparition |
|-----|-----------|----------------------|
| `npc-neutral-traveler` | Aucune | Fallback universel |
| `npc-silent-witness` | `PalaceRoomState.Silent` | Salles Silencieuses |
| `npc-desert-exile` | `RoomClimate: "Heatwave"` | Canicule |
| `npc-rage-beggar` | `PalaceRoomState.Enraged` | Salles Enragées |
| `npc-rain-memory-keeper` | `RoomClimate: "Rain"` | Pluie |

Si le contexte actif est `Silent`, le PNJ `npc-silent-witness` est sélectionné. Si le contexte est `Neutral`, le `npc-neutral-traveler` est sélectionné (fallback).

## Ce qui n'est pas implémenté (hors PR)

- Dialogues PNJ dynamiques (choix conditionnels, réponses)
- Rewards PNJ
- Relations joueur/PNJ (réputation, historique)
- Narration adaptative complète
- Élise adaptative en fonction du PNJ présent
- Frontend PNJ
- Palace Pressure complet
- Gestion de `Enraged` / `Violent` pour les candidats (`ToCandidateState` les force toujours à `Neutral`)

## Points ouverts

- `NpcNodeEventResolver` reste synchrone et générique ; c'est le pipeline asynchrone qui apporte l'adaptation. Si à l'avenir le résolveur synchrone a besoin de données Catalog, il faudra passer `Resolve` en async.
- Le `RunId`/`RoomId`/`NodeId` dans `NpcEligibilityContext` sont passés comme `Guid.Empty` par `NpcEventContentResolutionStrategy` (car le contexte de résolution de contenu n'a pas accès aux IDs). Cela n'affecte pas le déterminisme car la seed + le `NodeDepth` suffisent dans la configuration actuelle. À améliorer si besoin.
- `NpcNodeEventResolver` n'est plus le point d'entrée principal pour les PNJ ; l'essentiel de la logique est dans `NpcEventContentResolutionStrategy` (asynchrone).

## Validations exécutées

```
dotnet build services/game-engine/Leds.GameEngine.slnx    → 0 erreur
dotnet test services/game-engine/Leds.GameEngine.UnitTests → 1067 réussis, 0 échec, 16 ignorés

dotnet build services/catalog/Leds.Catalog.slnx           → 0 erreur
dotnet test services/catalog/Leds.Catalog.UnitTests       → 239 réussis, 0 échec
```

Aucun nouveau `Skip` ajouté.
