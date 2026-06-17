# alpha-0.8.1 — Active Laws & Curses

## Objectif

Exposer les influences actives d'une run (lois du Palais, malédictions, modificateurs)
dans un panneau dédié, accessible depuis le ruban de statut sans passer par l'écran Équipe.

---

## Composants modifiés

### `features/palace-laws/LawsPopover.vue` (modifié)

Étendu pour afficher les trois types d'influences en sections distinctes.

Props ajoutées :
- `curses?: ActiveCurseDto[] | null`
- `modifiers?: RunModifierDto[] | null`

Titre affiché changé de « Lois du Palais » vers **« Influences actives »**.

Trois sections conditionnelles :

| Section | Contenu | Affiché si |
|---------|---------|------------|
| Lois du Palais | nom, version, domaine (chip coloré), description | `laws.length > 0` |
| Malédictions | nom, sévérité (chip blood), description, durée, badge « Consommée » | `curses.length > 0` |
| Modificateurs actifs | valeur (+/-), type (label FR), durée, source | `modifiers.length > 0` |

État vide « Aucune influence active » affiché si les trois collections sont vides ou nulles.

Les lois utilisent un tableau (`v-for`) — l'architecture supporte nativement plusieurs lois
simultanées dès maintenant, même si le backend n'en envoie qu'une à la fois à ce stade.

---

### `features/runs/components/RunStatusRibbon.vue` (modifié)

- Emit `openInfluences` ajouté.
- Les chips de comptage (lois, modificateurs) sont convertis en `<button>` cliquables.
- Chip `activeCurses` ajoutée (`es-chip--blood`) si des malédictions sont actives.
- Cliquer l'un ou l'autre de ces chips déclenche `openInfluences`.

---

### `pages/RunPage.vue` (modifié)

- `LawsPopover` reçoit maintenant `:curses` et `:modifiers` depuis le store.
- `RunStatusRibbon` reçoit `@open-influences="uiStore.toggleLaws"`.

---

## Champs affichés

### `ActivePalaceLawDto`

| Champ | Affiché |
|-------|---------|
| `displayName` | Oui |
| `version` | Oui |
| `domain` | Oui — chip coloré (gold/frost/blood) |
| `description` | Oui |
| `key` | Non (clé interne) |

### `ActiveCurseDto`

| Champ | Affiché |
|-------|---------|
| `displayName` | Oui (fallback sur `curseDefinitionKey`) |
| `description` | Oui |
| `severity` | Oui — chip blood |
| `duration` | Oui — label FR |
| `consumedAtUtc` | Oui — badge « Consommée » + opacité réduite |
| `curseDefinitionKey` | Fallback uniquement |
| `id` | Non |

### `RunModifierDto`

| Champ | Affiché |
|-------|---------|
| `type` | Oui — label FR (dictionnaire) |
| `value` | Oui — signé (+/-), couleur sap/blood |
| `duration` | Oui — label FR |
| `sourceType` | Oui — label FR |
| `sourceKey` | Non |
| `id` | Non |

---

## Décisions techniques

**Architecture multi-lois.** `activePalaceLaws` est déjà un tableau dans le `RunDto`. Le
panneau itère dessus avec `v-for`. Aucune hypothèse mono-loi n'est faite — ajouter des lois
supplémentaires côté backend fonctionnera sans modification frontend.

**Pas de distinction bénéfique / négatif.** Les modificateurs sont colorés uniquement par
la valeur numérique (`+` → sap, `-` → blood), sans inférence de sémantique métier.

**`consumedAtUtc` affiché visuellement.** Une malédiction consommée (usage unique déjà
déclenché) est affichée en opacité réduite avec le badge « Consommée ». Elle reste visible
pour l'information de l'historique.

**Séparation conservée.** `PartyDrawer` continue d'afficher les influences en mode compact
(liste courte). `LawsPopover` est le panneau détaillé. Les deux coexistent sans conflit.

---

## Validations effectuées

- `npm run build` → ✓ 0 erreur TypeScript, 0 avertissement
- `npm run test` → ✓ 82 tests passent (dont 25 nouveaux `LawsPopover`)

---

## Points reportés

- Exposer une icône ou un motif visuel par domaine de loi (design non validé).
- Trier les influences par priorité (lois → malédictions → modificateurs) si le nombre
  devient important — déjà ordonné dans ce sens mais sans tri dynamique.
- Afficher l'historique des influences consommées en section « Archives » (hors scope).
