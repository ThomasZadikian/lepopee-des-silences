# alpha-0.8.1 - Palace Public Indicator Projection

## Objectif

Produire `RunDto.PalaceIndicators` depuis l'etat runtime public de la run, sans exposer les details techniques Markov, adaptatifs ou modifiers.

## Service ajoute

`IPalacePublicIndicatorProjectionService` / `PalacePublicIndicatorProjectionService` dans `Leds.GameEngine.Application.Runs.PalaceIndicators`.

Le service combine :

- les snapshots `PalaceIndicator` persistants deja existants ;
- les lois du Palais actives non consommees ;
- la malediction active non consommee ;
- le climat actif public de la room courante.

## Exposition API

`GetRunByIdQueryHandler` charge toujours les snapshots via `IPalaceIndicatorRepository`, puis appelle le service de projection pour construire la liste publique finale.

`RunDto.FromDomain` accepte maintenant une projection publique deja construite via `projectedPalaceIndicators`, afin d'eviter de serialiser des objets domaine internes.

## Regles Publiques

- Loi active : `category = law`, `source = law`.
- Malediction active : `category = curse`, `source = curse`.
- Climat de room : `category = climate`, `source = room`.
- Snapshot persistant : `source = run`.
- Les indicateurs expires sont filtres.
- Les doublons sont retires par couple `(key, source)`.

## Champs Interdits

La projection ne produit pas :

- poids ;
- probabilites ;
- coefficients ;
- matrices ;
- details Markov ;
- scores bruts ;
- scores adaptatifs ;
- `SourceDecisionId` ;
- `CreatedAtUtc` ;
- `ExpiresAtUtc` ;
- contenu de `RunModifier`.

## Limites Assumees

- Le climat est encore resolu depuis le DTO public actuel `RoomDto.ActiveClimate`, qui expose une string stable de type `Grey`, `Rain`, `Heatwave` ou `Hail`.
- Les descriptions runtime restent volontairement narratives et sans chiffres d'equilibrage.
- Les autres endpoints qui appellent `RunDto.FromDomain(run)` sans projection continuent de retourner une collection vide par defaut.

## Validations Effectuees

- `dotnet test services/game-engine/tests/Leds.GameEngine.UnitTests/Leds.GameEngine.UnitTests.csproj` : OK, `1030 passed / 16 skipped`.
- `dotnet test services/game-engine/tests/Leds.GameEngine.IntegrationTests/Leds.GameEngine.IntegrationTests.csproj --filter "FullyQualifiedName~GetRunByIdEndpointTests" -p:BaseOutputPath="C:\Users\Thomas\AppData\Local\Temp\opencode\game-engine-test-bin\"` : OK, `7 passed`.
- `dotnet build services/game-engine/Leds.GameEngine.slnx -p:BaseOutputPath="C:\Users\Thomas\AppData\Local\Temp\opencode\game-engine-build-bin\"` : OK.
- `dotnet build services/game-engine/Leds.GameEngine.slnx` en sortie standard : bloque par un processus `Leds.GameEngine.Api` existant qui verrouille les DLL de sortie.
