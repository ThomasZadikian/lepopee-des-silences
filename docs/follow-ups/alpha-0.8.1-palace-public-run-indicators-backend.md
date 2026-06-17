# alpha-0.8.1 - Palace Public Run Indicators Backend

## Probleme observe

La tentative frontend `feat(game-client): display palace public run indicators` a ete arretee parce que les DTOs publics `RunDto` et `RoomDto` n'exposaient aucun champ public d'indicateurs du Palais.

Le backend possedait deja des objets et repositories internes autour de `PalaceIndicator` et `AdaptiveInfluence`, mais aucune projection publique stable n'etait disponible pour le client.

## Decision

Exposer uniquement des indicateurs publics cures via une projection application explicite.

Les objets domaine `PalaceIndicator` ne sont pas serialises directement. Le DTO public est mappe par allowlist depuis les champs joueur-safe.

## DTO ajoute

`PalacePublicIndicatorDto` dans `Leds.GameEngine.Application.Runs.Dtos`.

Champs exposes :

- `Key`
- `Label`
- `Description`
- `Category`
- `Level`
- `Tone`
- `Source`

## Emplacement

Les indicateurs globaux de run sont exposes dans `RunDto.PalaceIndicators`.

`GET /api/v2/runs/{runId}` charge les indicateurs via `IPalaceIndicatorRepository` et les mappe dans `RunDto`.

Les autres projections `RunDto.FromDomain(run)` retournent une collection vide par defaut, sans simuler d'indicateurs.

## Champs interdits

La projection publique n'expose pas :

- Markov
- matrix
- weight
- probability
- coefficient
- raw score
- adaptive score
- value adaptative brute
- `SourceDecisionId`
- `CreatedAtUtc`
- `ExpiresAtUtc`
- `RunModifier`
- formule ou regle serveur interne

## Limites assumees

- Aucun nouvel indicateur metier public n'est cree dans cette PR.
- Les indicateurs expires sont filtres hors de la projection publique.
- `activeModifiers` reste une information technique separee et n'est pas utilise comme source d'indicateur public.
- `RoomDto` n'est pas enrichi pour le moment afin d'eviter de multiplier les emplacements.

## Validations effectuees

- `dotnet test services/game-engine/tests/Leds.GameEngine.UnitTests/Leds.GameEngine.UnitTests.csproj --filter "FullyQualifiedName~RunDtoPublicIndicatorTests|FullyQualifiedName~GetRunById"`
- `dotnet test services/game-engine/tests/Leds.GameEngine.IntegrationTests/Leds.GameEngine.IntegrationTests.csproj --filter "FullyQualifiedName~GetRunByIdEndpointTests"`
- `dotnet build services/game-engine/Leds.GameEngine.slnx`
- `dotnet test services/game-engine/Leds.GameEngine.slnx --no-build`

## Points reportes

- affichage frontend ;
- systeme complet de Palace Pressure ;
- enrichissement narratif par Elise ;
- animation visuelle ;
- equilibrage final ;
- exposition de nouveaux indicateurs metier publics.
