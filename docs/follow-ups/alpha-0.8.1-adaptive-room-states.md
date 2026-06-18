# alpha-0.8.1 - Adaptive Room States (PalaceRoomState)

## Probleme observe

Les Rooms Palace avaient une difficulte uniforme : aucun etat adaptatif ne differenciait une salle Silencieuse d'une salle Douloureuse. Le climat apportait des variations externes, mais chaque Room se comportait de maniere identique en combat, independamment de son contexte narratif.

## Decision

Introduire `PalaceRoomState`, un enum a 5 valeurs (`Neutral`, `Silent`, `Painful`, `Enraged`, `Violent`) dont seules les 3 premieres sont candidates actives. La resolution se fait via une matrice Markov deterministe dans `MarkovPalaceRoomStateResolver`, branchee dans `DeterministicRunGenerator.GenerateNextRoomAsync`.

L'etat est :

- stocke en base (`RoomEntity.PalaceState`)
- expose publiquement via `RoomDto.PalaceState` (DTO `PalaceRoomStateDto` avec allowlist `Key`, `Label`, `Description`, `Tone`)
- projete comme indicateur public dans `PalacePublicIndicatorProjectionService` (uniquement si non-Neutral)
- applique en combat via `CombatFactory.CreateFromDraft`

## Contrat de resolution

`PalaceRoomStateResolutionContext` :

- `Seed` : seed du run
- `MatrixVersion` : `Run.MarkovMatrixVersion` (persistee, pas la constante globale)
- `PreviousRoomState` : `run.CurrentRoom.PalaceState`
- `PreviousRoomType`, `NextRoomType`, `NextRoomDepth`
- `ActiveLawKeys`, `ActiveCurseKeys`, `ActiveClimate` (tries pour reproductibilite)

Matrice Markov privee (dans `MarkovPalaceRoomStateResolver`) :

- `Neutral` : 50% Neutral, 25% Silent, 25% Painful
- `Silent` : 35% Neutral, 45% Silent, 20% Painful
- `Painful` : 35% Neutral, 20% Silent, 45% Painful

Cas particuliers : Threshold / Depth=0 / Final → toujours Neutral.

`ToCandidateState` : `Enraged`/`Violent` → Neutral (definis mais non candidats).

## Effets combat

- `PalaceRoomState.Silent` : enemies recoivent +8 `startingGuard` et `baseGuard` (consomme par le systeme de Guard existant)
- `PalaceRoomState.Painful` : les skills enemies de type `Damage`/`DamageVitality` voient leur `BasePower` reduite de 10% (cumulatif avec le multiplicateur de climat)
- `PalaceRoomState.Neutral` : aucun effet

## Fichiers modifies/ajoutes

- `services/game-engine/src/Leds.GameEngine.Domain/Rooms/PalaceRoomState.cs` (nouvel enum)
- `services/game-engine/src/Leds.GameEngine.Domain/Rooms/Room.cs` (propriete `PalaceState`)
- `services/game-engine/src/Leds.GameEngine.Application/Runs/Dtos/RoomDto.cs` (`PalaceRoomStateDto`)
- `services/game-engine/src/Leds.GameEngine.Application/RoomMaps/IMapRoomGenerator.cs` (parametre `palaceState`)
- `services/game-engine/src/Leds.GameEngine.Infrastructure/Generation/RoomMaps/MapRoomGenerator.cs` (parametre accepte mais pas utilise)
- `services/game-engine/src/Leds.GameEngine.Application/Combats/ICombatFactory.cs` (parametre `palaceRoomState`)
- `services/game-engine/src/Leds.GameEngine.Application/Combats/CombatFactory.cs` (effets Silent/Painful)
- `services/game-engine/src/Leds.GameEngine.Domain/Combats/Combatant.cs` (`CreateEnemy` enrichi : `startingGuard`, `attackPower`, `defense`, `speed`)
- `services/game-engine/src/Leds.GameEngine.Infrastructure/Persistence/Entities/RoomEntity.cs` (colonne `PalaceState`)
- `services/game-engine/src/Leds.GameEngine.Infrastructure/Persistence/Configurations/RoomEntityConfiguration.cs` (mapping, default, index)
- `services/game-engine/src/Leds.GameEngine.Infrastructure/Persistence/Migrations/20260618120000_AddRoomPalaceState.cs`
- `services/game-engine/src/Leds.GameEngine.Infrastructure/Persistence/Mappers/RunPersistenceMapper.cs`
- `services/game-engine/src/Leds.GameEngine.Infrastructure/Generation/Rooms/States/IPalaceRoomStateResolver.cs` (nouveau)
- `services/game-engine/src/Leds.GameEngine.Infrastructure/Generation/Rooms/States/MarkovPalaceRoomStateResolver.cs` (nouveau)
- `services/game-engine/src/Leds.GameEngine.Infrastructure/Generation/DeterministicRunGenerator.cs` (integration + correction `Run.MarkovMatrixVersion`)
- `services/game-engine/src/Leds.GameEngine.Application/Runs/PalaceIndicators/PalacePublicIndicatorProjectionService.cs` (indicateur public room-state)
- `services/game-engine/src/Leds.GameEngine.Application/Runs/ResolveCurrentEvent/ResolveCurrentEventCommandHandler.cs` (passe `room.PalaceState` a `CombatFactory`)
- `services/game-engine/src/Leds.GameEngine.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` (DI `MarkovPalaceRoomStateResolver`)
- `services/game-engine/tests/Leds.GameEngine.UnitTests/Common/Factories/TestGeneratorFactory.cs` (resolver dans generator)
- `services/game-engine/tests/Leds.GameEngine.UnitTests/Combats/CombatFactoryTests.cs` (6 nouveaux tests)
- `services/game-engine/tests/Leds.GameEngine.UnitTests/Generation/DeterministicRunGeneratorTests.cs` (3 nouveaux tests)

## Tests

1039 tests unitaires passent (1030 existants + 9 nouveaux), 16 ignores (pre-existants).

Nouveaux tests :

- `GenerateInitialRoom_ShouldHaveNeutralPalaceState` : Threshold depth 0 → Neutral
- `GenerateNextRoom_ShouldResolvePalaceState` : etat valide (Neutral, Silent, Painful), jamais Enraged/Violent
- `GenerateNextRoom_ShouldBeDeterministic_ForPalaceState` : meme contexte → meme etat
- `CreateFromDraft_ShouldNotApplyPalaceGuard_WhenNeutral` : Neutral = guard 0
- `CreateFromDraft_ShouldApplySilentGuard_WhenSilent` : Silent = guard 8
- `CreateFromDraft_ShouldNotApplyAnyGuard_WhenSilent_AllySide` : allies non affectes
- `CreateFromDraft_ShouldReduceEnemyDamageSkill_WhenPainful` : -10% damage
- `CreateFromDraft_ShouldNotReduceEnemyGuardSkill_WhenPainful` : non-damage preserve
- `CreateFromDraft_ShouldApplyPainfulOverClimateMultiplier` : Painful + Heatway cumules

## Notes

- `Enraged` et `Violent` sont definis mais exclus du pool candidat : aucun effet minimal implemente.
- `Defense` n'est pas consomme par `ApplyDamage` : le bonus defense potentiel de Silent est visible via DTO mais pas mecanique.
- DOT/Poison/Bleed inexistant : `Painful` DOT skill bias reporte.
- L'Antechamber n'a pas de profil de generation dedie : fallback sur Threshold.
- `MarkovStateDistribution.Advance` jamais appele en production (infra/test only).
