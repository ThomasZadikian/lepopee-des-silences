# Brief Claude Design — murs, décor d'ambiance et échelle des salles d'exploration

> Document de transmission. Fait suite à l'audit technique du système de génération/rendu des
> salles d'exploration (2026-08) et à la décision de lever, pour ce périmètre précis, le gel de
> `tilecraft.js` acté dans `direction-visuelle-palais-respire.md` (voir sa section « Ce qui ne
> bouge pas », amendée). Les palettes/textures par salle déjà validées (voir
> `brief-design-par-salle.md`, les 27 salles canon) ne changent pas — ce brief porte sur ce qui
> s'ajoute à chaque salle, pas sur ce qui la teinte.
>
> **Portée** : la carte d'exploration uniquement (`tilecraft.js`). `bestiaire.js` (combat) n'est
> pas concerné — sa grammaire de lisibilité tactique reste un système à part.

---

## 1. Vision

Le Palais est plus grand à l'intérieur qu'il ne le laisse présager de l'extérieur — ce n'est pas
une incohérence narrative, c'est une propriété du lieu. L'objectif final : que les salles telles
qu'elles existent aujourd'hui (une unité de niveau, une bascule discrète entre deux salles — voir
plus bas) puissent aussi devenir, visuellement et structurellement, des paliers vastes et habités
plutôt que des plateaux ouverts parsemés de quelques rochers. La référence donnée est Diablo :
des espaces avec de vraies limites (quatre murs), du mobilier et du décor qui donnent le sentiment
d'un lieu vécu, pas vide.

Trois chantiers composent cette refonte, traités comme un seul ensemble (pas de séquençage forcé
entre eux) :

1. **Des murs qui se lisent comme des murs**, pas comme un rocher isolé.
2. **Du décor d'ambiance qui remplit l'espace** — pas seulement sur les cases de gameplay.
3. **Des salles à l'échelle d'un vrai lieu à explorer**, avec potentiellement des petites pièces
   à l'intérieur de la salle principale.

---

## 2. Ce qui existe déjà (pour ne pas repartir de zéro)

- **13 silhouettes d'obstacle** sont déjà peintes dans `tilecraft.js` (`monolith`, `shard`,
  `column`, `obelisk`, `trunk`, `shelf`, `rubble`, `boulder`, `deadfall`, `crates`, `gate`,
  `brokenColumn`, `spire`). Chaque salle n'en expose que **3** aujourd'hui via son tableau
  `walls`.
- **27 salles canon + 7 thèmes de repli** ont chacun leur palette (sol, mur, particule, ciel) —
  ce vocabulaire ne change pas, voir `brief-design-par-salle.md`.
- **Un tableau `props` par salle existe déjà** (15 sortes au total : colonnes, troncs, cairns,
  arches, faisceaux lumineux, etc.) mais **n'est jamais peint en semis d'ambiance** — il n'est
  utilisé que pour la case exacte d'un nœud de gameplay. C'est du contenu déjà écrit, prêt à être
  activé, pas à inventer.
- Le sol peut déjà être une forme irrégulière (alcôves, bords en L) — la génération grignote la
  silhouette du rectangle englobant. Ce n'est pas un partitionnement en chambres, mais la brique
  de base (croissance de blob + vérification de connectivité) existe et est réutilisable.

---

## 3. Ce qu'on attend de Claude Design

### 3.1 Murs

Aujourd'hui un « mur » est un obstacle isolé (un rocher, une colonne) posé au hasard — rien ne le
distingue visuellement d'un simple blocage de case. Pour qu'une enceinte à quatre murs se lise
comme une vraie limite de pièce, il faut probablement des **pièces orientées** plutôt qu'une
silhouette unique répétée :
- un segment de mur « droit » (lisible en ligne continue quand plusieurs cases s'alignent),
- une pièce d'angle,
- une pièce de seuil/porte (l'ouverture dans l'enceinte).

Décor par thème/salle inchangé (la silhouette de mur suit toujours la palette de la salle), mais
le vocabulaire de formes s'enrichit. Le placement (où ces pièces vont dans la grille) est un
problème de génération, pas de dessin — voir §4.

### 3.2 Décor d'ambiance

Activer le tableau `props` existant en semis sur les cases de sol libres (pas seulement la case
d'un nœud), avec une densité qui laisse la salle lisible et navigable. Deux besoins :
- Si les 15 sortes déjà déclarées suffisent en variété, **aucun nouvel asset requis** — juste un
  nouveau point de rendu à écrire (ingénierie, voir §4).
- Si certaines salles au texte canon distinctif (voir `brief-design-par-salle.md`) méritent un
  décor plus spécifique que les entrées génériques actuelles (`column`, `beam`, `cairn`,
  `obeliskProp`, `arch`, `trunk`), c'est le moment de les enrichir salle par salle plutôt que
  d'ajouter un vocabulaire générique de plus.

### 3.3 Échelle et petites pièces internes

Moins un besoin d'assets qu'un besoin de **pièces de mur compatibles avec un partitionnement** :
si un chantier futur découpe la salle principale en chambres reliées par des seuils, les pièces du
§3.1 doivent pouvoir composer une enceinte fermée avec une ouverture, pas seulement border le
pourtour extérieur de la salle. Pas de nouvelle demande de fond ici au-delà de ce que le §3.1
fournit déjà, si les pièces sont pensées composables dès le départ.

---

## 4. Ce qui reste côté ingénierie (pas une demande à Claude Design)

Pour que le périmètre soit clair : la composition des murs en enceintes (placement, connectivité,
seuils), le semis de `props` en rendu, le partitionnement en chambres, et l'agrandissement de la
grille (actuellement 26×18 — plafonné par le pathfinder O(n²) et par la validation `[6,30]` nœuds
de `Run.StartNew`) sont des chantiers de génération et de moteur, pas des livrables visuels. Ils
seront traités séparément une fois le vocabulaire visuel de ce brief disponible.

---

## 5. Contraintes à respecter

- **La salle reste l'unité de niveau.** Pas de fusion de plusieurs salles en un monde continu —
  la bascule entre salles reste une transition discrète (comme les étages de Diablo lui-même,
  reliés par des escaliers). Les petites pièces internes du §3.3 vivent **à l'intérieur** d'une
  salle, elles ne relient pas deux salles entre elles.
- **Palette figée par salle.** Les 27 identités déjà validées (`brief-design-par-salle.md`) ne se
  renégocient pas ici — les nouvelles pièces de mur/décor suivent la palette existante de chaque
  salle, elles n'en introduisent pas une nouvelle.
- **Le ban-list esthétique du Palais tient toujours**, même si ce n'est pas le système
  « Palais respire » (chrome hors-combat) qui gouverne ce brief : pas de rouages, pas d'or/laiton,
  pas d'architecture gothique à arches, pas de décor steampunk. Le ton reste celui déjà établi par
  les 27 salles et leurs textes canon.
- **`bestiaire.js` et la grille de combat ne sont pas concernés.** Rien dans ce brief ne touche au
  système de couleur fonctionnelle du combat tactique.
