# alpha-0.8.1 Room Climate Frontend

## Objectif

Afficher dans `apps/game-client` le climat actif de la Room courante sans déplacer de logique métier côté Vue.

## DTO Consommé

Source principale : `GET /api/v2/runs/{runId}` via `RunDto.currentRoom`.

Champs lus côté frontend :

- `currentRoom.activeClimate`
- `currentRoom.climate` en alias défensif si exposé plus tard

Le backend expose actuellement `activeClimate` comme type technique texte (`Grey`, `Rain`, `Heatwave`, `Hail`) ou `null`. Le frontend accepte aussi un objet DTO futur contenant uniquement des champs publics optionnels : `key`, `type`, `displayName`, `description`, `source`, `expiresAt`, `expiresWhen`, `roomId`.

## Composants Créés Ou Modifiés

- Créé : `features/room-climate/RoomClimateBadge.vue`
- Créé : `features/room-climate/RoomClimatePanel.vue`
- Créé : `features/room-climate/roomClimateDisplay.ts`
- Modifié : `features/runs/components/RunStatusRibbon.vue`
- Modifié : `features/palace-laws/LawsPopover.vue`
- Modifié : `pages/RunPage.vue`
- Modifié : `features/runs/types/runTypes.ts`

## Champs Affichés

- Nom public du climat.
- Description publique si fournie par le DTO, sinon fallback textuel minimal pour le type technique.
- Source si fournie.
- Durée si fournie via `expiresWhen` ou `expiresAt`, sinon `Room actuelle uniquement`.
- `roomId` si fourni.

## Décision

Aucun effet de climat n’est calculé côté Vue. Le frontend n’applique aucun changement de force, vitesse, soin ou DOT. Le helper `roomClimateDisplay.ts` ne fait qu’un mapping textuel d’affichage lorsque le backend ne fournit qu’un type technique.

## Limites Assumées

- Les descriptions fallback sont volontairement générales et ne contiennent pas de coefficients.
- La source et l’expiration détaillée ne sont visibles que si le backend les expose.
- `activeClimate` reste la source de vérité publique.

## Validations Effectuées

- `npm run build` : OK.
- `npm run test` : OK, `138 passed`.
- `npm run type-check` : script absent de `apps/game-client/package.json`. Le typage est couvert par `vue-tsc -b` exécuté dans `npm run build`.
- `dotnet build services/game-engine/Leds.GameEngine.slnx` : OK avec warnings existants.
- `dotnet test services/game-engine/Leds.GameEngine.slnx --no-build` : OK, unit `1011 passed / 16 skipped`, integration `78 passed`.

## Points Reportés

- Animation visuelle des climats.
- Effets graphiques avancés.
- Intégration complète au combat UI.
- Sélection automatique des climats par type de Room.
- Équilibrage final.
