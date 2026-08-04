# Brief Claude Design — résoudre les nœuds en superposition, plus en écrans dédiés

> Document de transmission, dans la continuité de `brief-direction-artistique-ui-ux.md` (§2.6 y
> appelait déjà « un seul cadre de modale »). Celui-ci est plus étroit et plus opérationnel :
> il décrit, élément par élément, **où** chaque pièce doit apparaître à l'écran et **comment**
> elle doit se comporter, pour l'implémentation. Le style visuel (matières, palette, ornements)
> est déjà fixé par les briefs précédents — celui-ci ne le redécide pas, il pose la mécanique
> d'apparition/position par-dessus.
>
> **Rien n'est implémenté à ce stade.** Ce document sert de base à l'implémentation par Claude
> Design des maquettes déjà produites.

---

## 0. Le changement de fond

Aujourd'hui, résoudre un nœud **remplace entièrement l'écran** : `RunPage.vue` bascule son
contenu principal d'un état de jeu à l'autre (`gameplayPhase`), et la carte d'exploration
disparaît complètement du DOM pendant qu'un panneau plein cadre prend sa place. Neuf composants
Vue distincts jouent ce rôle aujourd'hui : `RewardOfferPanel`, `NpcDialoguePanel`,
`LawResolutionPanel`, `MerchantPanel`, `EventOutcomePanel` (générique), `EventChoiceResultPanel`
(deux occurrences), `InterludePanel`, `RoomClearedPanel`.

**La nouvelle règle** : la carte reste montée à l'écran, **assombrie et bloquée**, et la
résolution du nœud s'affiche **par-dessus**, en superposition. Le joueur ne quitte jamais la
pièce dans laquelle il se trouve pour un événement qui se joue dans cette même pièce.

Cette règle s'applique à **tous les nœuds d'exploration** : Présence (PNJ), Marchand, Décret du
Palais (Loi), Malédiction, Repos, Objet, Souvenir. Elle exclut explicitement :

- **Combat** (`TacticalCombatScene`) — nécessite un vrai changement de scène (grille tactique,
  jetons 3D), hors périmètre.
- **Salle nettoyée** / **Interlude** (`RoomClearedPanel`, `InterludePanel`) — ce sont des
  transitions de **changement de salle**, pas des résolutions de nœud ; l'Interlude est de toute
  façon prévu à la suppression (déjà demandé).
- **Suspendue** / **Terminée** / **Sélection d'objet permanent** — états de fin de run, pas des
  nœuds.

---

## 1. Trois formes, pas neuf écrans

C'est le point le plus important pour réduire le nombre d'écrans : en observant le contenu réel
de chacun des neuf panneaux actuels, il n'existe que **trois formes distinctes** de contenu. Les
neuf composants doivent devenir trois gabarits de superposition réutilisés avec un contenu
différent, pas neuf mises en page différentes.

| Forme | Contenu | Nœuds concernés |
|---|---|---|
| **A — Dialogue** | Une conversation qui s'écrit au fil de l'échange | Présence (PNJ) |
| **B — Résolution** | Un titre, un texte, éventuellement un choix à faire (grille d'objets, sceau, tirage) | Marchand, Loi, Malédiction, Repos, Objet, Souvenir |
| **C — Écho** | Confirmation courte du résultat d'un choix, avant de rendre la main | Suite de la forme B (Marchand, Loi, Malédiction, Repos, Souvenir) — **jamais** après Objet/PNJ |

Ce découpage vient directement du code existant : `handleEventContinue`/`handleSelectChoice`
dans `RunPage.vue` font systématiquement transiter Marchand, Loi, Malédiction, Repos et Souvenir
par un écran de confirmation (`EventChoiceResultPanel`, ou sa version « transition synthétique »)
avant de rendre la main — c'est donc une étape partagée, pas un écran par nœud. À l'inverse,
Objet (qui offre un choix de récompense parmi plusieurs cartes) et Présence (dialogue) rendent la
main directement à la fin de leur propre séquence, sans passer par cet écho.

---

## 2. Le mécanisme commun aux trois formes

Un seul comportement à construire, réutilisé partout :

- **Fond assombri** : la carte reste visible mais s'assombrit (même traitement que la modale de
  confirmation déjà en place sur le Seuil — fond `oklch(0.14 0.030 272 / 0.72)` avec flou léger —
  c'est la seule modale du jeu aujourd'hui, elle devient la référence pour toutes les autres).
- **Carte bloquée** : aucun clic sur la grille, aucun déplacement, aucune ouverture de tiroir
  (Besace/Influences/Équipe) ni action sur le ruban de statut de bas d'écran tant qu'une
  superposition de nœud est active. Le micro-menu d'équipe (bouton flottant, toujours au-dessus
  de tout aujourd'hui) : à trancher — soit il reste actionnable (équiper un objet permanent reste
  possible), soit il se désactive comme le reste. Recommandation : le laisser actif, comme
  pendant le combat aujourd'hui (justifié par le même commentaire dans le code : équiper doit
  rester accessible en toutes circonstances).
- **Apparition** : la superposition entre par un fondu + léger mouvement (jamais un claquement
  sec — voir la règle de mouvement du brief UI/UX général, §2.4 : tout bouge lentement, comme le
  Palais lui-même).
- **Sortie** : la superposition se referme, l'assombrissement se dissipe, la carte redevient
  interactive — jamais de rechargement de la carte elle-même (elle n'a jamais quitté le DOM).

---

## 3. Forme A — Dialogue (Présence / PNJ)

Reprend l'idée déjà validée : le PNJ apparaît sur le fond assombri, et les répliques s'empilent
en historique plutôt que de s'écraser l'une l'autre.

- **Position de l'entité** : centrée, partie haute de l'écran, au-dessus de la zone de dialogue —
  garde le traitement déjà écrit dans `NpcDialoguePanel` (silhouette + nom au-dessus).
- **Position du fil de dialogue** : ancré bas-centre de l'écran. Aujourd'hui une seule boîte
  écrase son propre texte à chaque nouvelle réplique (`NpcDialoguePanel`, `watch(dialogue,
  startNode)` réinitialise tout) — **c'est ce comportement qui change**. Chaque réplique devient
  une bulle qui reste affichée ; une nouvelle réplique/un nouveau choix pousse les précédentes
  vers le haut (translation, léger fondu/estompage sur les plus anciennes pour garder l'attention
  sur la plus récente) plutôt que de les remplacer.
- **Choix** : apparaissent sous la dernière bulle (comme aujourd'hui), et une fois choisis,
  deviennent eux-mêmes une bulle dans l'historique (côté joueur, visuellement distincte de celles
  du PNJ — alignement ou teinte différente) avant que la réplique suivante n'apparaisse.
- **Sortie** : bouton « Se retirer » (déjà existant) referme la superposition. Pas d'écran
  d'écho — le dialogue se referme sur lui-même.

---

## 4. Forme B — Résolution (Marchand, Loi, Malédiction, Repos, Objet, Souvenir)

Une carte centrée sur l'écran, sur fond assombri — jamais plein cadre. Le contenu interne varie
selon le nœud, mais le cadre extérieur (position, taille, apparition) est unique.

- **Position** : centrée, largeur variable selon le contenu (voir ci-dessous), jamais plus large
  que ~900px pour rester lisible comme un objet posé sur la carte plutôt que comme un nouvel
  écran.
- **Marchand** (`MerchantPanel`) : grille d'objets à acheter + monnaie affichée en tête. Contenu
  le plus large des six (grille de cartes) — garder une largeur proche de l'actuel.
- **Loi** (`LawResolutionPanel`) : déjà construit autour d'un sceau centré (`SealGlyph`) + texte
  de la loi à droite — se prête bien à une carte plus étroite, format portrait/carré plutôt que
  large.
- **Malédiction, Repos, Souvenir** (aujourd'hui `EventOutcomePanel` générique) : titre + texte
  narratif + éventuellement un tiroir de choix qui s'ouvre vers le bas (mécanisme déjà existant
  dans `EventOutcomePanel`, à garder tel quel mais contenu dans la carte plutôt qu'en plein
  cadre).
- **Objet** (`RewardOfferPanel`) : grille de cartes de récompense à choisir — même famille visuelle
  que Marchand (grille), largeur comparable.
- **Comportement commun** : un seul choix possible par apparition de la carte (acheter un objet,
  sceller une loi, choisir une récompense, valider un geste) — la carte se referme après la
  validation, elle ne reste jamais ouverte pour un second choix dans la même apparition.

---

## 5. Forme C — Écho (confirmation courte)

La plus petite des trois formes, et la plus fréquente : elle suit presque tous les nœuds de la
forme B (jamais après Objet ni Présence, voir §1).

- **Position** : carte compacte, centrée, nettement plus petite que la forme B — pense à un
  médaillon de confirmation plutôt qu'à un panneau (le contenu réel aujourd'hui :
  `EventChoiceResultPanel` — un kicker, une puce de résultat, un court label, un bouton
  « Continuer »).
- **Enchaînement** : apparaît immédiatement après la fermeture de la carte de résolution (forme
  B), sans repasser par la carte assombrie/débloquée entre les deux — l'assombrissement reste
  continu du début de la résolution jusqu'à la fermeture de l'écho.
- **Sortie** : bouton « Continuer » referme tout, la carte redevient claire et interactive.

---

## 6. Ce qui ne bouge pas

- Le moteur de rendu de la carte (`tilecraft.js`) reste vendoré et non modifié — l'assombrissement
  est une couche par-dessus (un calque d'opacité), pas une modification du moteur.
- Combat, changement de salle (Interlude/Salle nettoyée) et états de fin de run gardent leur
  traitement plein cadre actuel — voir §0.
- Le style visuel de chaque panneau (couleurs, ornements, sceau, grilles) est déjà fixé par les
  briefs précédents — ce document ne redécide que la position et le comportement d'apparition.
