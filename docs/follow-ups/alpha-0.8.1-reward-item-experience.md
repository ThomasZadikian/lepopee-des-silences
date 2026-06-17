# alpha-0.8.1 — Reward & Item Experience

## Objectif

Améliorer l'affichage et l'utilisation des données publiques de récompenses et
d'objets de run exposées par le Game Engine.

---

## Endpoints consommés

| Méthode | Endpoint | Usage |
|---------|----------|-------|
| `GET`  | `/api/v2/runs/{runId}/rewards/pending` | Charger la reward offer en attente |
| `POST` | `/api/v2/runs/{runId}/rewards/select`  | Sélectionner un choix de reward |
| `GET`  | `/api/v2/runs/{runId}`                 | Fallback si la réponse select ne contient pas le RunDto |

---

## Composants créés / modifiés

### `features/rewards/components/RewardOfferPanel.vue` (modifié)

- Affiche le **label de source** (`NodeEvent` → "Événement", `Combat`, `Elite`, `RoomBoss`, `Rare`).
- Affiche un **chip d'état** si la reward est `Selected` ou `Expired`.
- Correction de la détection de ton (`getTone`) : `MemoryFragment` → frost, `StatBonus`/`TemporaryItem` → gold.
- Les `rewardType` backend (`Heal`, `TemporaryItem`, `StatBonus`, `MemoryFragment`) sont traduits en labels lisibles par le joueur.
- L'état `Expired` / `Selected` désactive les interactions et affiche une note.
- Le bouton de confirmation est masqué pour les offers déjà résolues.

### `features/rewards/components/RunItemCard.vue` (créé)

Composant présentationnel réutilisable pour un seul `RunItemDto`.

Props :
- `item: RunItemDto` — l'objet de run depuis le DTO serveur
- `compact?: boolean` — mode compact (cache la description)

Emits :
- `use(itemId: string)` — déclenché uniquement si `item.isUsable === true`

Affiche : nom, quantité, type, rareté (chip coloré), description, badge d'effet (ton coloré), bouton "Utiliser" si `isUsable` est explicitement `true`.

---

## Champs reward affichés

| Champ DTO backend | Affiché |
|-------------------|---------|
| `id` | Utilisé comme clé de sélection |
| `source` | Oui — label traduit |
| `state` | Oui — chip si Selected/Expired |
| `choices[].id` | Clé de sélection |
| `choices[].rewardType` | Oui — label traduit + détection de ton |
| `choices[].label` | Oui |
| `choices[].description` | Oui |
| `choices[].payloadKey` | Non affiché (interne) |
| `selectedChoiceId` | Non affiché directement |
| `combatScaling` | Non affiché (données internes) |

---

## Champs item affichés

| Champ DTO | Affiché |
|-----------|---------|
| `displayName` | Oui |
| `description` | Oui (caché en mode compact) |
| `type` | Oui |
| `rarity` | Oui — chip coloré (sap/frost/gold) |
| `quantity` | Oui si > 1 |
| `effectType` | Oui — badge traduit avec ton |
| `effectAmount` | Oui — inclus dans le badge |
| `isUsable` | Bouton "Utiliser" affiché si `true` |
| `definitionKey` | Non affiché |

---

## Décisions techniques

**Aucun effet n'est calculé côté Vue.** Le composant affiche uniquement les
valeurs `effectType` et `effectAmount` exposées par le serveur, sans simulation
ni reconstruction de résultat.

**`isUsable` est optionnel.** Le backend `RunDto.inventoryItems` n'expose pas
`isUsable` directement. Le champ est `boolean | undefined` dans le type
frontend. Le bouton "Utiliser" n'apparaît que si `isUsable === true` est
explicitement défini par le serveur (ex : endpoint `/inventory`).

**`selectReward` utilise la réponse directement.** Le store utilise maintenant
`unwrapRunFromSelectRewardResponse` pour extraire le `RunDto` depuis la réponse
de `POST /rewards/select` (`{ run, rewardOffer }`), évitant un appel GET
supplémentaire. Fallback : GET si la réponse ne contient pas le run.

**`SelectRewardRequest` simplifié.** Le corps envoyé au backend est
`{ choiceId: string }` uniquement, aligné sur le contrat C# `SelectRewardRequest(Guid ChoiceId)`.

---

## Limites assumées

- `combatScaling` n'est pas affiché (données internes de difficulté/scaling).
- L'effet item n'est pas prévisualisé : seul le label `+N Vitalité` etc. est montré.
- Les runs antérieures sans `isUsable` dans le RunDto ne montrent pas de bouton "Utiliser" — comportement attendu.
- `RunItemCard` n'est pas encore intégré dans `InventoryDrawer` / `InventoryPanel` (composants stables existants, refactoring hors scope).

---

## Validations effectuées

- `npm run build` → ✓ 0 erreur TypeScript, 0 avertissement
- `npm run test` → ✓ 57 tests passent (dont 13 nouveaux `RewardOfferPanel` + 19 nouveaux `RunItemCard`)

---

## Points reportés

- Intégrer `RunItemCard` dans `InventoryDrawer` et `InventoryPanel` (alpha-0.8.2).
- Exposer `isUsable` dans `RunDto.inventoryItems` côté backend pour unifier les deux endpoints (backend task).
- Afficher `combatScaling.riskBand` comme indicateur de difficulté visible pour le joueur (si décision design validée).
