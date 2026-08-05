# Direction visuelle — "Le Palais respire"

> **Référence unique et définitive pour toute l'UI hors-combat**, à partir du 2026-08. Remplace
> intégralement la direction précédente (`brief-direction-artistique-ui-ux.md`, désormais marqué
> comme remplacé) : plus de rouages, plus d'or/laiton, plus d'architecture gothique à arches, plus
> de décor steampunk. Validée sur l'écran Le Seuil avec Claude Design, produite par
> `docs/design/../` (maquettes `.dc.html` reçues du designer).
>
> **Portée** : cette direction couvre le *chrome* de l'interface — panneaux, boutons, cartes,
> modales, fonds d'écran, texte — sur tous les écrans hors combat (Seuil, fiches de personnage,
> tiroirs, superposition de nœuds, transitions de fin de run, etc.). Elle **ne remplace pas** la
> palette fonctionnelle du combat tactique (couleurs de surbrillance de case, silhouettes de
> créatures, registres de sorts — voir `brief-direction-artistique-combat.md` et
> `brief-design-par-salle.md`), qui reste un système à part, motivé par la lisibilité tactique et
> non par le décor. Le combat garde par exemple ses teintes bleu allié / rouge ennemi / or curseur
> / corail attaque — ce n'est pas un oubli, c'est un système différent qui répond à un besoin
> différent (voir Combat tactique.dc.html, où le chrome suit cette direction mais la grille de jeu
> garde sa propre grammaire de couleur).
>
> **Toute référence à l'ancienne direction (tokens `--gold`/`--blood`/`--sap`/`--frost` de
> `tokens.css`, composant `PalaceAtmosphere.vue`, teinte or de `SealGlyph.vue`) doit disparaître du
> code au fil de l'implémentation de chaque écran** — pas en un seul balayage global (trop risqué
> sans vérification visuelle par écran), mais systématiquement à chaque écran repris.

---

## Palette (noir froid + un seul accent pastel)

| Rôle | Token | Valeur |
|---|---|---|
| Fond général | `--void` | `#0c0d12` |
| Cartes/panneaux | `--panel` | `#15171a` |
| Survol/relief | `--panel-2` | `#1c1f24` |
| Texte principal | `--ink` | `#f2f1ee` |
| Texte atténué | `--ink-2` | `#d6d3cc` |
| Texte discret | `--ink-3` | `#a6a39c` |
| Texte très discret | `--ink-4` | `#726f6a` |
| Texte quasi invisible | `--ink-5` | `#4a4744` |
| Bordure | `--line` | `rgba(190,190,190,.24)` |
| Bordure discrète | `--line-soft` | `rgba(190,190,190,.14)` |
| **Accent unique** | `--mint` | `#bfe3e0` |
| Accent atténué | `--mint-dim` | `#8fc4bf` |
| Alerte/perte | `--danger` | `#c07268` |
| Alerte atténuée | `--danger-dim` | `#8a4c45` |
| Ton secondaire neutre (perte de réputation, jamais un danger) | `--mauve` / `--mauve-dim` | `#c9c2e8` / `#b6acdc` |

> Révisé (2e passe Claude Design) : les valeurs `--ink-*`/`--line*`/`--panel-2`/`--danger` ont été
> éclaircies d'un cran sur l'ensemble des écrans pour le contraste — c'est un correctif de
> lisibilité globale, pas un changement de direction. `--mauve` est apparu à cette passe : un ton
> secondaire pour ce qui est triste/mélancolique sans être une alerte (perte de réputation) —
> distinct de `--danger`, qui reste réservé à ce qui menace/coûte concrètement.

`--mint` est **le seul accent vif** de toute l'interface hors-combat — réservé aux interactions
actives, à l'information positive, aux transitions/passages. **Ne jamais l'utiliser en décor.**
`--danger` est réservé à l'alerte/la perte, jamais décoratif non plus.

Les glyphes de registre émotionnel (✶ ◇ ❍ ⟡ ◈ ○ ✳, `EmotionalTypeBadge`) restent transversaux et
gardent leurs propres teintes ponctuelles — ce sont des glyphes d'information, pas de la
décoration, donc ils ne sont pas concernés par la règle du mint unique.

## Typographie

- Titres : **Newsreader**, italique, poids 400. Jamais en gras.
- UI/corps : **IBM Plex Sans**, 400/500/600.
- Chiffres/données/seeds : **IBM Plex Mono**.

(Remplace `Playfair Display` / `Inter` / `JetBrains Mono` de l'ancien système.)

## Matière vivante

Le Palais est un corps qui respire, pas une machine :

- Légère pulsation d'échelle sur le calque de fond (`scale(1) → scale(1.007)`, ~11s,
  `cubic-bezier(0.5,0,0.5,1)`).
- Veines fines aux bords : tracés SVG fins en `--line`, avec un segment en `--mint-dim` qui
  voyage lentement le long du tracé (`stroke-dasharray` + `dashoffset` animé).
- Grain permanent en soft-light, très léger.
- **Explicitement écarté** : rouages, or/laiton, architecture gothique à arches, tout décor
  "steampunk" — donc tout ce que `PalaceAtmosphere.vue` peint aujourd'hui (rouages, cœur battant,
  vapeur, gouttières dorées, magma) est à retirer, pas à adapter.

## Composants communs

- **Bouton/lien actif** : jamais de fond coloré — le texte et le glyphe passent en `--mint` au
  survol, transition lente (`.5–.6s cubic-bezier(0.5,0,0.5,1)`). `SealGlyph.vue` (le sceau animé,
  actuellement doré) est à retravailler dans cet esprit le jour où son écran (Loi) est repris —
  pas de fond, pas d'or, l'accent est le seul signal.
- **Modale unique** : panneau `--panel`, bordure 1px `--line`, titre Newsreader italique, jamais
  de coins arrondis prononcés (0–2px de rayon). C'est le même gabarit que celui déjà retenu pour
  la superposition des nœuds (`brief-superposition-noeuds.md`) — un seul cadre de modale pour tout
  le jeu, cohérent avec les deux documents.
- **Transition plein écran** (changement de salle/écran majeur) : flood radial `--mint` → `--void`
  (vu sur Le Seuil : `radial-gradient(circle at 50% 55%, var(--mint) 0%, var(--void) 74%)`).

## Ce qui ne bouge pas

- Le moteur de rendu `tilecraft.js`/`bestiaire.js` (carte + combat) reste vendoré tel quel, non
  modifié — confirmé par les maquettes Claude Design elles-mêmes, qui le réutilisent à l'identique.
- La grammaire de couleur fonctionnelle du combat (surbrillances de case, silhouettes, registres de
  sorts) — voir portée ci-dessus.
- La mécanique de superposition des nœuds (3 formes, carte assombrie/bloquée) — seul son habillage
  visuel change.

## Suivi de la migration

Implémentés dans le code (2026-08) : Le Seuil, le cadre persistant de la run (GameShellLayout,
GameTopBar), la carte d'exploration (chrome), la superposition des nœuds (3 formes — Présence en
bulles empilées, Marchand/Loi/Malédiction/Repos/Objet/Souvenir en carte centrée, Écho en médaillon
compact), Statuts, Manifestations, Réputation (barre + score, offrandes retirées de la vue joueur),
Tutoriel, Équipe fusionnée (Équipe/Statistiques/Grimoire/Équipement/Besace en un seul écran à
onglets — `TeamHubPage.vue`), Salle nettoyée, Sélection d'objet permanent, Run terminée/suspendue,
tous les tiroirs et popovers (Besace, Influences, Équipe-en-run, Journal, ruban de statut,
micro-menu, overlay Elise, popup de réputation, diptyque de décision). Les sections Malédiction ont
été retirées d'Influences et d'Équipe-en-run (retrait volontaire, confirmé).

`PalaceAtmosphere.vue` et `RuleOrnament.vue` sont supprimés (plus aucun écran ne les utilise) ;
`LivingWalls.vue` et les tokens `void/panel/mint/mauve/danger` sont la seule référence en vigueur
pour tout ce qui précède.

Encore sur l'ancienne direction, explicitement hors périmètre en attente d'un élément externe :
- **Combat tactique (HUD)** — attend une maquette révisée (garde/mana/focus/statuts) ; le chrome des
  panneaux Claude Design fournis est déjà obsolète sur ce point précis.
- **Bestiaire en écran joueur** — attend un scoping (aujourd'hui un outil de production interne, pas
  un écran destiné au joueur).
- La grammaire de couleur fonctionnelle du combat elle-même (surbrillances de case, silhouettes,
  registres de sorts, `StatusEffectToken` en combat) reste hors périmètre par nature, voir
  « Ce qui ne bouge pas » ci-dessus — pas un report, une exclusion de fond.
