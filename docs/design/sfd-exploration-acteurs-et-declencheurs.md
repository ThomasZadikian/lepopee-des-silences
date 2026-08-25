# SFD — Exploration, acteurs mobiles et déclencheurs

Statut : **référence d'implémentation**
Référence : **LEDS-SFD-EXP-001**
Version : **1.0 — 25/08/2026**

Ce document fixe le comportement des personnages non-joueurs, des ennemis et
des événements dans les salles d'exploration. Il complète la SFD combat
canonique et prévaut sur les comportements historiques qui déplaçaient les PNJ
au rythme des pas du joueur.

## 1. Objectif d'expérience

Le Palais doit paraître habité sans donner l'impression que le joueur commande
le monde entier à chacun de ses pas. Un acteur mobile se déplace d'une case à la
fois, avec une animation lisible. Le mouvement du joueur reste prioritaire : les
PNJ neutres attendent la fin de son animation, tandis que les ennemis conservent
une réaction autonome et menaçante.

## 2. Vocabulaire

- **PNJ neutre** : acteur de décor vivant, de dialogue ou de confirmation ; il
  ne déclenche pas de combat par simple proximité.
- **Ennemi d'exploration** : représentation mobile d'un groupe de combat
  ordinaire dans la salle.
- **Événement scénarisé** : règle locale, zone, séquence ou conséquence de
  dialogue pouvant lancer un combat sans ennemi mobile.
- **Contact** : entrée du joueur sur la case ennemie ou arrivée de l'ennemi au
  voisinage immédiat du joueur lors de sa réaction.
- **Distance** : distance de Manhattan `|Δx| + |Δy|` ; aucune diagonale.
- **Tick d'acteurs** : étape atomique durant laquelle chaque acteur éligible
  peut parcourir au plus une case.

## 3. Règles fonctionnelles

### 3.1 Priorité du joueur

- **RG-EXP-001** — Aucun PNJ neutre ne commence ou ne poursuit un déplacement
  pendant l'animation de déplacement du joueur.
- **RG-EXP-002** — Un tick neutre ne peut être demandé que lorsque la salle est
  en exploration libre, sans dialogue, confirmation, événement, récompense ou
  combat en cours.
- **RG-EXP-003** — Après la fin d'un déplacement joueur qui n'a déclenché aucun
  événement, les ennemis obtiennent une phase de réaction. Cette phase empêche
  de neutraliser leur poursuite en enchaînant les déplacements.

### 3.2 PNJ neutres

- **RG-EXP-010** — Un PNJ neutre éligible se déplace au plus d'une case par tick
  lorsque le joueur est immobile.
- **RG-EXP-011** — Dès que sa distance au joueur est inférieure ou égale à deux
  cases, le PNJ cesse tout déplacement. Il reste immobile tant que cette
  condition est vraie.
- **RG-EXP-012** — L'interaction volontaire est disponible à une case de
  distance. Le joueur ne pénètre pas sur la case du PNJ.
- **RG-EXP-013** — Les archétypes `Fixed` et `Guardian` conservent leur poste.
  `Patrol`, `Passive` et `Hunter` suivent leur comportement authored uniquement
  hors de la zone d'arrêt de deux cases.
- **RG-EXP-014** — Deux PNJ, un PNJ et un ennemi, ou un PNJ et le joueur ne
  peuvent jamais terminer un tick sur la même case.

### 3.3 Ennemis d'exploration

- **RG-EXP-020** — Tous les nœuds de combat ordinaires (`Combat`, `Rare`,
  `Elite`, `RoomBoss`, `FinalBoss`) sont des ennemis d'exploration et se
  déclenchent par contact.
- **RG-EXP-021** — À une distance inférieure ou égale à trois cases, l'ennemi
  choisit une case orthogonale réduisant la distance au joueur.
- **RG-EXP-022** — À plus de trois cases, l'ennemi peut errer d'une case selon
  une décision déterministe. Il n'est pas couplé au nombre de cases parcourues
  par le joueur.
- **RG-EXP-023** — Un ennemi se déplace pendant les ticks d'inactivité et durant
  sa phase de réaction après déplacement joueur. Il reste donc libre de
  poursuivre le joueur.
- **RG-EXP-024** — Le contact sélectionne immédiatement la rencontre et ouvre le
  combat T-RPG, sans fenêtre de confirmation supplémentaire.
- **RG-EXP-025** — La case de contact constitue le centre de déploiement du
  combat tactique conformément à la SFD combat canonique.
- **RG-EXP-026** — Un ennemi résolu, caché, verrouillé ou déjà sélectionné ne se
  déplace pas.

### 3.4 Événements et scénarios

- **RG-EXP-030** — Les combats produits par une règle locale, un événement, un
  dialogue ou une séquence de scénario ne sont pas convertis en ennemi mobile.
- **RG-EXP-031** — Les déclencheurs `ZoneEntry`, `NpcInteraction`, choix et
  seuils de sévérité restent souverains. Cela inclut les protocoles du Hall
  d'entrée, le tapis et les interactions avec les Émotions.
- **RG-EXP-032** — Une interaction avec un PNJ ou un élément demandant une
  confirmation met le monde en pause jusqu'à la fermeture ou la résolution de
  l'interface.

### 3.5 Animation et lisibilité

- **RG-EXP-040** — Chaque déplacement d'acteur est interpolé entre sa case de
  départ et sa case d'arrivée ; aucun changement de position ne doit apparaître
  comme une téléportation.
- **RG-EXP-041** — Les départs de plusieurs acteurs sont légèrement décalés afin
  d'éviter un mouvement parfaitement simultané de la salle entière.
- **RG-EXP-042** — Les entrées joueur sont verrouillées durant l'animation d'un
  tick d'acteurs.
- **RG-EXP-043** — L'option d'accessibilité « réduire les animations » conserve
  l'ordre et les règles de simulation, mais réduit la durée visuelle.

## 4. Parcours utilisateur

1. La salle se charge avec ses PNJ, ennemis, événements et objets persistés.
2. Le joueur reste immobile : un tick fait évoluer les acteurs autorisés.
3. Le joueur clique une destination : les PNJ neutres sont figés, le joueur
   parcourt son itinéraire puis termine son animation.
4. Si le joueur entre en contact avec un ennemi, la rencontre s'ouvre.
5. Sinon, les ennemis jouent une réaction atomique ; un contact éventuel ouvre
   la rencontre.
6. À deux cases d'un PNJ, celui-ci attend. À une case, le joueur peut déclencher
   l'action d'interaction.
7. Toute interface modale suspend les ticks jusqu'au retour à l'exploration.

## 5. Critères d'acceptation

- **CA-EXP-001** — Un PNJ `Passive` ne change pas de case pendant que le jeton
  joueur est animé.
- **CA-EXP-002** — Un PNJ neutre situé à distance deux reste immobile pendant
  au moins trois ticks successifs.
- **CA-EXP-003** — Un clic sur un PNJ adjacent appelle l'interaction sans tenter
  de déplacer le joueur sur sa case.
- **CA-EXP-004** — Un ennemi à distance trois réduit la distance d'une case lors
  de sa réaction, sous réserve de collision.
- **CA-EXP-005** — Un ennemi adjacent déclenche le combat par contact.
- **CA-EXP-006** — Un combat produit par `ZoneEntry` dans le Hall ne crée aucun
  ennemi mobile.
- **CA-EXP-007** — Aucun acteur ne traverse une case non marchable et aucune
  paire d'acteurs ne partage une case après un tick.
- **CA-EXP-008** — Les positions sont identiques après sauvegarde/reprise.
- **CA-EXP-009** — Chaque mouvement visible suit une interpolation et non un
  saut de rendu.

## 6. Hors périmètre

- Refonte des dialogues ou du contenu narratif des PNJ.
- Navigation diagonale, vitesse variable ou trajectoires multi-cases par tick.
- Changement du système de combat T-RPG après le contact.
- Remplacement des déclencheurs de scénario existants.

## 7. Traçabilité

| Besoin | Règles | Vérification |
|---|---|---|
| Supprimer l'effet de téléportation | RG-EXP-040 à 043 | tests du plan de rendu + test manuel |
| Découpler les PNJ du joueur | RG-EXP-001 à 003, 010 | tests domaine et store |
| Permettre l'approche d'un PNJ | RG-EXP-011 à 014 | tests interaction/collision |
| Généraliser les combats au contact | RG-EXP-020 à 026 | tests domaine/API |
| Préserver les scénarios du Hall | RG-EXP-030 à 032 | tests protocoles locaux |
