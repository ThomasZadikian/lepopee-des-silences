# Prompt — une salle d'exemple : murs réels, entrée, décor habité

> Suite à `brief-murs-et-decor-exploration.md`. Avant de traiter les 27 salles, on veut valider le
> vocabulaire visuel sur **une seule salle d'exemple**, poussée à fond. Une fois validée, elle sert
> de référence pour décliner le reste.

---

## La demande

Produis **une salle d'exemple complète**, dans le même format que tes maquettes précédentes
(`.dc.html`), qui démontre en une seule scène :

1. **De vrais murs**, pas des obstacles isolés. Aujourd'hui un « mur » dans le jeu est un rocher
   ou une colonne posée seule au milieu du sol — rien ne le relie à ses voisins ni ne le fait lire
   comme une limite de pièce. Sur cette salle d'exemple, les murs doivent former une **enceinte
   reconnaissable** : des segments qui s'alignent en ligne continue, un traitement d'angle là où
   deux murs se rencontrent, pas un semis aléatoire.
2. **Une entrée identifiable.** Une ouverture nette dans l'enceinte — un seuil, une porte, un
   passage — qui se distingue clairement d'un mur plein au premier coup d'œil, pas une case de sol
   qui ressemble au reste.
3. **Un décor diversifié, habité.** Au-delà des murs et du sol : du mobilier/décor d'ambiance
   dispersé dans l'espace (pas uniquement sur les cases de gameplay), en plusieurs silhouettes
   différentes plutôt qu'un seul élément répété partout. L'objectif : que la pièce se lise comme
   un lieu vécu, pas comme un plateau vide avec quelques accidents de terrain.

## La salle à utiliser comme support

**Hall d'entrée** (`room.halldentree`) — c'est la toute première salle du jeu, sa description
canon s'y prête directement :

> « Depuis toujours le Palais a su accueillir ses invités. Couvert d'un grand tapis rouges et
> habillé de quatre merveilleux piliers de marbre, le Hall d'entrée du Palais n'est que la
> représentation de l'arrogance de son propriétaire. Une fois traversé, rares sont les personnes
> qui ont eu l'occasion de le revoir. »

Éléments déjà fixés pour cette salle (ne pas réinventer) :
- Sol : tapis (`carpet`), rouge (`rug: #8e2f36`).
- Palette : `top #b9a377`, `accent #e8c069`, `glow #fff1c4`, thème de base Antichambre.
- Les **quatre piliers de marbre** du texte canon : à traiter comme du **décor intérieur**
  (mobilier dispersé dans la pièce), distincts des murs d'enceinte — ce sont deux choses
  différentes dans cette salle (les colonnes ne sont pas les murs qui la ferment).
- Les murs d'enceinte, eux, peuvent reprendre le vocabulaire déjà associé à cette salle
  (`column`/`gate`/`brokenColumn`) mais traité en pièces orientées (voir point 1) plutôt qu'en
  silhouettes isolées.

## Ce qu'on ne demande PAS ici

- Pas les 27 salles — une seule, poussée à fond, pour valider l'approche avant de décliner.
- Pas de nouvelle palette — celle du Hall d'entrée est déjà validée, on l'applique.
- Pas de resserrer les règles produites plus tôt (rouages/or-laiton/architecture gothique à
  arches/steampunk restent hors périmètre — voir `direction-visuelle-palais-respire.md`).
- Pas de toucher à `bestiaire.js` ni à la grille de combat.

## Contraintes techniques à respecter

- Géométrie de tuile existante : diamant isométrique 2:1, `TILE = { W: 128, H: 64, STEP: 20,
  MAX: 3 }` (4 paliers d'élévation, 0 à 3) — la salle d'exemple doit rester lisible dans cette
  grammaire, pas dans une projection différente.
- La grille de jeu actuelle fait 26×18 cases ; la salle d'exemple n'a pas besoin de remplir tout
  cet espace pour la démonstration — un extrait représentatif (murs + entrée + décor visibles
  ensemble) suffit tant que l'échelle reste cohérente avec le reste du jeu.

## Une fois la salle validée

Elle devient la référence pour :
- décliner le même traitement sur les 27 salles canon (chacune garde sa propre palette, voir
  `brief-design-par-salle.md`),
- et pour l'équipe d'ingénierie, qui reprend le vocabulaire de formes pour écrire la génération
  procédurale (composition des murs, placement de l'entrée, connectivité) — travail séparé, qui
  vient après.
