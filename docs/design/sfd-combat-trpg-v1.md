# SFD — Mode de combat T-RPG (v1.0)

## 1. Contexte et objectif

Le jeu dispose aujourd'hui d'un unique système de combat : ATB (barre de temps continue) avec
positionnement abstrait (rangs Front/Back, sans coordonnées). Cette SFD introduit un **second**
système de combat, de type T-RPG tactique sur grille (tour par tour, déplacement, portée,
zone d'effet), **sans remplacer** le système existant.

**Principe directeur, non négociable** : les deux systèmes de combat coexistent indéfiniment.
Le joueur choisit celui qu'il veut utiliser au moment de lancer une run, sur l'écran de
sélection des runs. Ce choix est global à la run entière (aucun changement de mode en cours
de run) et n'affecte **aucun** autre système du jeu (progression, récompenses, catalogue,
lois du Palais, etc.) — seule la manière de jouer les combats change.

## 2. Sélection du mode de combat

- Nouveau choix exposé sur l'écran de sélection/lancement d'une run : **Classique (ATB)** ou
  **Tactique (T-RPG)**.
- Le choix est stocké sur la `Run` dès sa création et reste fixe pour toute sa durée.
- Aucun mécanisme de changement de mode en cours de run n'est prévu (hors scope v1 — et
  probablement jamais, puisque ça impliquerait de re-belligérer tout l'état de combat/position
  d'un système vers l'autre).
- Toute la couche hors-combat (carte de salle, nœuds, récompenses, PNJ, marchands, Lois du
  Palais, réputation, monnaie) reste strictement identique quel que soit le mode choisi, **sauf**
  la section 3 ci-dessous qui ne s'applique qu'au mode Tactique.

## 3. Exploration en mode Tactique

- Une salle devient une **grille commune bornée** (dimensions variables selon le type de salle),
  parcourue librement par le groupe plutôt que représentée en graphe de nœuds abstrait.
- **Groupe = un seul token/sprite** hors combat (pas d'affichage individuel des personnages tant
  qu'aucun combat n'est engagé) — plus simple, cohérent avec l'absence d'art dédié du jeu.
- **Budget de déplacement** limité par salle (nombre de cases parcourables). Fixe en v1 ;
  l'augmentation via objets/mécanismes (équipement, compagnon, effet de Loi du Palais) est prévue
  mais **hors scope v1** — l'API/le modèle ne doit pas empêcher cette extension plus tard.
- **Nœuds = tokens sur la grille** (Combat/Elite/Rare/RoomBoss/FinalBoss/Item/Npc/Memory/Rest/
  Merchant/Law/Curse), qui n'apparaissent que lorsque le groupe est à portée de vue (brouillard
  de guerre). Le brouillard de guerre révèle aussi progressivement le terrain déjà exploré.
- **Nœuds principaux vs optionnels** : au moins un nœud (typiquement RoomBoss/FinalBoss, ou tout
  nœud marqué comme obligatoire) doit être atteint pour permettre la sortie de la salle. Les
  autres nœuds sont optionnels — visitables ou non selon le budget de déplacement restant et le
  choix du joueur.
- **Aucun retour possible** une fois la salle quittée : cohérence stricte avec le roguelike
  existant. Un nœud non visité par manque de budget est perdu définitivement à la sortie de la
  salle, exactement comme un nœud non choisi l'est aujourd'hui dans le système par arborescence.
- **Hauteur de terrain sur la carte principale** : purement cosmétique (aucun effet mécanique
  hors combat).
- Le brouillard de guerre ouvre la possibilité de récompenses d'exploration dédiées (objets
  cachés, fragments narratifs) — le catalogue précis de ces récompenses est **hors scope** de
  cette SFD et fera l'objet d'un travail de contenu séparé.

## 4. Déclenchement et placement du combat

- Le combat se joue sur la **même grille** que l'exploration (pas d'écran séparé, pas de zoom
  vers un autre espace) — mais les unités sont **repositionnées** au moment du déclenchement :
  - **Allié** : placés dans une **zone de déploiement dédiée**, proche de la position
    d'exploration du groupe au moment du déclenchement (pas de placement aléatoire pur, pour
    éviter qu'un placement malchanceux désavantage injustement une composition d'équipe).
  - **Ennemis** : placés selon leur position/formation de groupe déjà définie par le système de
    groupes cohérents du Bestiaire (Phase 14) — les ennemis d'une même bande apparaissent groupés
    plutôt que dispersés aléatoirement.
- Le déplacement en combat doit rester **réel et signifiant** : la zone de déploiement ne doit pas
  placer les deux camps si proches que le repositionnement devient inutile.

## 5. Déroulement du tour

- **Tour par tour par vitesse individuelle** (pas d'ATB, pas de phase "tout un camp puis
  l'autre") : chaque combattant (allié ou ennemi) agit dans un ordre mêlé déterminé par sa stat
  de Vitesse — cohérent avec le rôle déjà joué par cette stat côté ATB.
- **Chaque personnage du groupe est contrôlé individuellement** par le joueur, tour par tour —
  pas de mouvement d'équipe groupé (jugé sans intérêt pour la taille de groupe du jeu).
- **Déplacement + action fusionnés en une seule interaction** : le joueur clique une case de
  destination, un menu contextuel d'action s'ouvre directement sur place (plutôt que deux phases
  séparées "se déplacer" puis "agir"). Objectif explicite : éviter que le mouvement représente une
  part disproportionnée du temps de jeu par tour.

## 6. Mouvement

- **Portée de déplacement** = base fixe par archétype (ex. Bruiser/Guard plus lents, Skirmisher
  plus mobile) **+ bonus/malus dérivé de la stat Vitesse** (par rapport à une référence à
  calibrer à l'implémentation — moyenne du groupe ou seuils fixes). Le detail exact de la formule
  (coefficients) est **à trancher à l'implémentation**, pas figé par cette SFD.
- Volontairement **pas** une moyenne de toutes les stats du personnage (rejeté en conception :
  pénalise la spécialisation sans lien avec le rôle de l'archétype).
- Modificateurs de portée de déplacement via équipement : **hors scope v1**, mais le modèle ne
  doit pas bloquer cette extension.

## 7. Compétences — portée et zone d'effet

- **Portée = valeur définie par compétence** (pas uniquement dérivée de la Catégorie
  Physique/Magique, bien que la Catégorie existante puisse fournir une valeur par défaut à
  l'authoring pour limiter le travail de contenu initial).
- Modificateurs de portée via équipement (armes) : prévus pour une évolution future
  (`EquipmentEffectsJson` existe déjà et suit ce même patron pour d'autres effets), **hors scope
  v1**.
- **Zone d'effet** : forme fixe par compétence (ex. croix/losange centré sur la cible), taille
  variable selon la compétence (le "5 cases" évoqué en conception n'est qu'un exemple, pas une
  valeur figée). Formes alternatives (ligne, cône, zone libre) : **hors scope v1**, évolution
  envisageable plus tard.
- **Compétences "toute la carte"** : catégorie à part, symétrique — touchent absolument tout le
  monde présent sur la grille, alliés compris, sans distinction de camp, que l'effet soit
  offensif (dégâts) ou bénéfique (soin). Assumé comme un vrai levier de risque pour les
  compétences offensives de ce type (elles blessent aussi son propre camp).
- Les modes de ciblage existants (SingleEnemy, AllEnemies, AllAllies...) doivent être réinterprétés
  spatialement en mode Tactique : "AllEnemies" devient "tous les ennemis dans la zone d'effet",
  pas "tous les ennemis de la carte" (sauf pour les compétences "toute la carte" explicitement
  définies comme telles).

## 8. Terrain et hauteur (combat)

- **Cases infranchissables** : bloquent le déplacement, doivent aussi être prises en compte dans
  les calculs de ligne de vue/portée le cas échéant.
- **Hauteur pleinement mécanique** (contrairement à la carte principale, purement cosmétique) :
  affecte la ligne de vue, la portée effective, et un bonus/malus de dégâts selon la différence
  d'altitude entre attaquant et cible.
- **Effets de terrain** (au-delà de la hauteur et des cases infranchissables) : le principe est
  acté, mais le catalogue précis des effets est **hors scope** de cette SFD — à spécifier
  séparément avant implémentation de ce volet.

## 9. Paliers de risque et grille de combat

- Le palier de risque (Calme/Tendu/Dangereux/Périlleux/Fatal, déjà livré) influence désormais
  **aussi** la grille de combat en mode Tactique — taille de la grille, sévérité du terrain,
  distance de déploiement entre alliés et ennemis — en plus des multiplicateurs déjà en place
  (stats, butin, réputation, Éclats du Palais).
- Le mécanisme de mise "provoquer le destin" (déjà livré) reste identique dans son
  fonctionnement (le joueur monte le palier d'un nœud de combat disponible avant d'y entrer,
  répétable jusqu'à Fatal) ; ses effets se répercutent simplement aussi sur la grille de combat
  quand le mode Tactique est actif.
- Les valeurs précises (dimensions de grille par palier, sévérité du terrain par palier) sont
  **à définir à l'implémentation** sous forme de tables `// BALANCE KNOB`, suivant la convention
  déjà en place pour les autres multiplicateurs de palier.

## 10. IA ennemie

- **v1** : le ciblage réutilise le système d'Attitude et les groupes cohérents du Bestiaire déjà
  en place (poids combinés : faiblesse de type/catégorie **et** préférence d'archétype). La
  cible est choisie **sans tenir compte de la portée**, puis le mouvement se contente d'exécuter
  le déplacement nécessaire pour l'atteindre (ou de rester sur place si déjà à portée).
- **v2 (hors scope v1)** : la portée pourrait contraindre le choix de cible lui-même (ne
  considérer que les cibles atteignables ce tour-ci).
- **Cohésion de groupe** : les ennemis d'un même groupe cherchent à rester à portée de
  compétence de leurs alliés (utile notamment pour les archétypes de soutien qui veulent garder
  leurs cibles de soin/buff à portée).

## 11. Conditions de victoire, défaite et fuite

Inchangées : identiques au système ATB actuel (victoire par défaite de tous les ennemis, défaite
par défaite de tous les alliés). Aucune nouvelle condition (objectif d'évasion, protection d'unité,
survie N tours) n'est introduite par cette SFD.

## 12. Ce qui ne change pas

- Le système ATB existant reste **entièrement fonctionnel et inchangé** pour les joueurs qui
  choisissent le mode Classique — aucune régression attendue ni tolérée sur ce mode.
- Aucune récompense, mécanique de progression, système de Lois du Palais, monnaie, réputation, ou
  contenu catalogue n'est affecté par le choix de mode de combat.

## 13. Hors scope pour la v1 (explicitement reporté)

- Mouvement d'équipe par phase (rejeté définitivement, pas seulement reporté).
- Contrainte de portée dans le choix de cible de l'IA ennemie (v2).
- Formes de zone d'effet alternatives (ligne, cône, zone libre) au-delà de la forme fixe (v2).
- Modificateurs de portée de compétence/déplacement via équipement.
- Mécanismes d'augmentation du budget de déplacement d'exploration.
- Catalogue détaillé des effets de terrain (au-delà de hauteur et cases infranchissables).
- Nouvelles conditions de victoire/fuite.
- Changement de mode de combat en cours de run.

## 14. Points restant à trancher à l'implémentation (ne bloquent pas cette SFD)

- Formule exacte du bonus/malus de mouvement dérivé de la Vitesse (coefficients précis).
- Dimensions de grille et sévérité de terrain par palier de risque (tables `BALANCE KNOB`).
- Détail de l'algorithme de placement en zone de déploiement (rayon exact, règles de collision).
- Répartition précise des poids de ciblage IA (type vs archétype) dans le calcul de score.
