# alpha-0.8.1 - Adaptive Room Context → Enemy Selection

## Probleme observe

Le `PalaceRoomState` et le `RoomClimate` etaient transmis au `CombatFactory` pour les modifiers combat (guard Silent, -dmg Painful), mais pas au pipeline de selection ennemie. Chaque combat `Palace` utilisait donc la meme composition d'ennemis quel que soit l'etat de la salle : une salle Silencieuse (defensive) pouvait tirer des Bruisers agressifs, et une salle Violente (agressive) pouvait tirer des Guards defensifs.

## Decision

Etendre `EncounterCompositionContext` avec `PalaceRoomState` et `RoomClimate`, puis ajouter un tri par archetype prefere dans `EncounterCompositionPolicy.FilterEligibleEnemies` pour biaiser la selection vers des profils coherents avec l'etat de la salle.

Le biais est au niveau de l'archetype (pas du skill individuel), par choix de stabilite et de parcimonie : les archetypes sont stables, presents sur chaque ennemi, et ne necessitent pas d'extension du Catalog.

## Contrat de propagation

1. `ResolveCurrentEventCommandHandler` resolve `PalaceRoomState` (depuis `room.PalaceState`) et `RoomClimate` (via `ResolveActiveClimate`)
2. Ces valeurs sont passees dans `CombatEncounterDraftContext` (interne, pas expose dans le draft final)
3. `CombatEncounterDraftGenerator` les transfere dans `EncounterCompositionContext`
4. `EncounterCompositionPolicy` les utilise pour ordonner les candidats

## ArchetypePreferenceByState

- `Silent` (defensif) → Guard, Support
- `Painful` (debuff, attrition) → Disruptor
- `Enraged` (agressif) → Bruiser, Skirmisher
- `Violent` (degats directs) → Bruiser, Fragile
- `Neutral` → aucune preference (ordre historique preserve)

L'ordre final dans `FilterEligibleEnemies` : cout asc → archetype prefere (0/1) → difficulte desc → cle asc.

La preference agit comme un tri binaire : les archetypes prefetes passent devant les autres, mais tous les ennemis eligibles restent candidats (pas de filtrage, pas d'exclusion).

## Fichiers modifies

- `services/game-engine/src/Leds.GameEngine.Application/Combats/EncounterDrafts/CombatEncounterDraftContext.cs` (ajout `PalaceRoomState`, `RoomClimate`)
- `services/game-engine/src/Leds.GameEngine.Application/Combats/EncounterComposition/EncounterCompositionContext.cs` (ajout `PalaceRoomState`, `RoomClimate`)
- `services/game-engine/src/Leds.GameEngine.Infrastructure/Combats/EncounterDrafts/CombatEncounterDraftGenerator.cs` (propagation des deux champs)
- `services/game-engine/src/Leds.GameEngine.Infrastructure/Combats/EncounterComposition/EncounterCompositionPolicy.cs` (`ArchetypePreferenceByState` + tri)
- `services/game-engine/src/Leds.GameEngine.Application/Runs/ResolveCurrentEvent/ResolveCurrentEventCommandHandler.cs` (`ResolveActiveClimate` + passage)

## Tests

1267 tests unitaires passent (1039 game engine + 228 catalog), 16 ignores (pre-existants).

Pas de nouveaux tests dedies : la preference est testee indirectement par les tests de composition existants (l'ordre des ennemis reste stable et deterministe pour un meme contexte).

## Notes

- Le biais est effectif des que `PalaceRoomState != Neutral`. En l'etat, Silent et Painful sont les seuls etats actifs (Enraged/Violent forces a `Neutral` par `ToCandidateState`).
- DOT/Poison/Bleed inexistant : `Painful` avec Disruptor est le meilleur equivalent sans DOT.
- RoomClimate est transmis mais pas encore utilise comme critere de selection (reserve pour usage futur).
- `Enraged`/`Violent` ont leurs preferences definies en anticipation de leur activation.
