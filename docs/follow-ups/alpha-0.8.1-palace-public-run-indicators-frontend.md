# alpha-0.8.1 - Palace Public Run Indicators Frontend

## Objectif

Afficher cote joueur les indicateurs publics du Palais exposes par le Game Engine, sans reconstruire ni inferer la logique interne d'adaptation.

## DTO consomme

Source unique : `run.palaceIndicators`.

Type frontend ajoute : `PalacePublicIndicatorDto` dans `apps/game-client/src/features/runs/types/runTypes.ts`.

## Composants crees/modifies

- `apps/game-client/src/features/palace-indicators/PalacePublicIndicatorsPanel.vue`
- `apps/game-client/src/features/palace-indicators/PalaceIndicatorCard.vue`
- `apps/game-client/src/features/palace-laws/LawsPopover.vue`
- `apps/game-client/src/pages/RunPage.vue`

## Champs affiches

- `key`
- `label`
- `description`
- `category`
- `level`
- `tone`
- `source`

## Decisions

- Affichage public uniquement : le panneau ne lit que `run.palaceIndicators`.
- Aucun calcul cote Vue : le frontend ne derive pas d'intensite, de pression ou de danger.
- `activeModifiers` n'est pas utilise comme source du panneau d'indicateurs publics.
- Aucun Markov, poids, probabilite, coefficient ou score brut n'est affiche.

## Limites assumees

- Aucun nouvel indicateur n'est cree cote client.
- Le panneau affiche un etat vide si le backend ne fournit aucun indicateur public.
- L'integration reste dans le popover `Influences actives` pour eviter une refonte UX.
- Les donnees techniques deja presentes ailleurs dans l'interface ne sont pas reinterpretees comme indicateurs publics.

## Validations effectuees

- `npm run build`
- `npm run test`
- `dotnet build services/game-engine/Leds.GameEngine.slnx`
- `dotnet test services/game-engine/Leds.GameEngine.slnx --no-build`

Note : aucun script `type-check` n'existe dans `apps/game-client/package.json`. Le type-check Vue est inclus dans `npm run build` via `vue-tsc -b`.

## Points reportes

- systeme complet de Palace Pressure ;
- animation visuelle des indicateurs ;
- enrichissement narratif par Elise ;
- redesign UX du Palais ;
- nouveaux indicateurs backend.
