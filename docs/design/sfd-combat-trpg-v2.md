# SFD — Mode de combat T-RPG (v2.0)

> Remplace `sfd-combat-trpg-v1.md`. La v1 a été écrite avant trois chantiers qui l'ont
> périmée par endroits : la suppression du mode d'exploration Classique, l'arrivée du terrain
> mécanique en exploration, et l'arrivée du système de rangs Front/Back. Les sections
> correspondantes sont ici réécrites, pas amendées.

---

## 1. Contexte et objectif

Le jeu dispose aujourd'hui d'un système de combat unique : **ATB** (barre de temps continue),
avec un positionnement abstrait à deux valeurs — rangs `Front`/`Back`. Ce système est mature :
tempo vivant, coût d'investissement, momentum, statuts, DoT, garde continue, résistances typées,
catégories Physique/Magique, Attitudes du Bestiaire, paliers de risque.

Cette SFD introduit un **second** système de combat, tactique sur grille, tour par tour.
Elle ne remplace pas l'ATB.

### Ce qui a changé depuis la v1

| Hypothèse v1 | État réel |
|---|---|
| Le joueur choisit Classique/Tactique au lancement d'une run | Le véhicule n'existe plus — `RunExplorationMode` a été supprimé avec le mode d'exploration Classique. Il n'y a aucun `CombatMode` en base. |
| La hauteur est cosmétique sur la carte principale | Faux depuis le chantier Terrain : l'élévation coûte du déplacement et bloque la ligne de vue **en exploration**. |
| (rien sur les rangs) | Les rangs `Front`/`Back` sont livrés — 16 points d'appel, une commande `Reposition`, et plusieurs Lois du Palais qui les manipulent. |

---

## 2. Principe d'indépendance — la contrainte structurante

**Les deux systèmes de combat ne dépendent l'un de l'autre en aucune manière.** Le tactique
n'est pas dérivé de l'ATB, ne le spécialise pas, ne le configure pas. Aucun `if (mode == ...)`
dans le code de l'un pour servir l'autre.

Cette indépendance porte sur le **déroulé**, pas sur la **résolution**. La distinction est
non négociable, parce que l'appliquer à la résolution signifierait dupliquer la formule de
dégâts, les statuts et le Bestiaire — deux copies qui divergeraient au premier correctif.

### Strictement séparé

- L'ordonnancement (tempo ATB ↔ initiative tactique)
- Le modèle spatial (rangs ↔ coordonnées)
- L'économie d'action
- L'agrégat qui porte l'état du combat

`TacticalCombat` est un agrégat **frère** de `Combat`, pas un héritier ni une branche interne.

### Strictement commun

Un noyau de résolution : *« ce combattant utilise cette compétence sur ces cibles — que se
passe-t-il ? »*. Dégâts physiques et magiques, application et tick des statuts, DoT, garde
continue, soins en % de PV max, réductions typées, catégories, critiques et échecs.
Les deux systèmes l'appellent ; **aucun ne le possède**.

### Conséquence de chantier

Ce noyau est aujourd'hui enchevêtré dans `Combat.cs` (867 lignes, ~34 points de contact ATB)
et `Combatant.cs` (945 lignes, ~33 points de contact). **Son extraction est le premier
chantier, et tout le reste en dépend.**

Le dossier `Domain/Combats/Atb/` (10 fichiers : scheduler, formules de tempo, momentum, jitter)
est déjà correctement isolé et reste intouché.

---

## 3. Sélection du mode de combat

> ✅ **Tranché et implémenté.** Champ `Run.CombatMode` (`Atb` | `Tactical`), choisi au lancement
> de la run et fixe pour toute sa durée.

C'est un **nouveau** champ, distinct du `RunExplorationMode` supprimé : l'exploration reste
uniformément sur grille dans les deux cas.

Câblage livré : `RunCombatMode` (domaine) → `Run.StartNew` / `Rehydrate` → colonne `combat_mode`
(migration `AddRunCombatMode`, texte, défaut `Atb`) → `StartRunCommand` → `POST /runs` →
`RunDto.CombatMode`. Omettre le champ donne l'ATB, le système historique.

Les runs antérieures à ce choix se relisent en ATB — la valeur par défaut de la colonne le dit
explicitement, et la relecture retombe sur `Atb` pour toute valeur inconnue.

Aucun changement de mode en cours de run n'est prévu. Toute la couche hors-combat (grille
d'exploration, nœuds, brouillard, fouille, récompenses, PNJ, marchands, Lois, réputation,
monnaie, Éclats) est **rigoureusement identique** dans les deux modes.

---

## 4. Le champ de bataille

Le combat se joue sur **la grille d'exploration elle-même** — pas d'écran séparé, pas de
transition vers un autre espace.

**Au chargement du combat, la grille est vidée de tous ses nœuds**, résolus ou non. Il ne reste
que la matière brute du terrain :

- la forme de la salle (les salles non rectangulaires sont livrées) ;
- les cases infranchissables et les trous ;
- l'élévation.

C'est cette matière brute qui fait varier le terrain d'un combat à l'autre : la diversité vient
gratuitement de la génération procédurale existante, sans authoring dédié.

À la fin du combat, les nœuds sont restaurés dans leur état d'avant — le combat n'altère pas
la progression d'exploration de la salle.

### Surface disponible

Une salle générée pour freiner l'exploration peut ne pas offrir assez de cases libres pour
déployer 4 alliés et 5 ennemis à distance signifiante.

Deux réponses, complémentaires :

1. **Agrandissement général des salles** (décidé, dépasse le cadre de cette SFD — touche aussi
   l'exploration).
2. **Écran déroulant** : la carte peut excéder la surface visible, la caméra suit l'unité active.
   Lève le plafond de taille imposé par le viewport.

**Garde-fou minimal exigé pour cette v2** : la génération garantit une surface libre contiguë
suffisante pour le déploiement des deux camps au palier de risque de la salle. À défaut, un
déploiement dégradé (rayon réduit) plutôt qu'un échec.

---

## 5. Effectifs

| | Avant | Après |
|---|---|---|
| Personnages jouables | 5 | **4** — sur les deux systèmes |
| Ennemis | 3 max (palier 5) ; Elite/Boss/Rare = 1 | **5 max**, selon la difficulté |

### Passage à 4 personnages

Ce changement **touche l'ATB existant**, pas seulement le tactique. La limite actuelle est
`MaxPartySize = 5` (`Application/DevTools/DevToolsRunDebugService.cs`). À vérifier :

- le recrutement de compagnons et ses plafonds ;
- le sort d'une run en cours dont l'équipe est déjà à 5 ;
- l'affichage ATB, dimensionné pour 5.

### Passage à 5 ennemis

L'échelle actuelle est dans
`Infrastructure/Combats/EncounterDrafts/DeterministicEncounterEnemySelector.cs` :
`{1:1, 2:1, 3:2, 4:2, 5:3}`, avec `Elite`/`RoomBoss`/`Rare` forcés à 1.

**Livré** : l'escorte d'Elite suit désormais le palier de risque comme une rencontre ordinaire —
l'Elite occupe une place, le reste de l'effectif autorisé peut l'accompagner. Elle était bornée à
un seul accompagnateur, ce qui rendait un nœud Elite systématiquement moins fourni qu'un combat
normal de même palier. La règle « strictement plus faible que l'Elite » est conservée : l'escorte
ne doit jamais transformer le nœud en combat à deux menaces équivalentes.

> **Tranché : plafond commun aux deux systèmes.** La composition de la rencontre est décidée en
> amont du combat, avant que le mode n'entre en jeu ; le rendre dépendant du mode obligerait à
> faire descendre `Run.CombatMode` jusque dans la génération de brouillon, pour une différence
> d'un seul ennemi au seul palier Fatal. L'échelle réelle est
> `EncounterCompositionPolicy.GetMaxEnemiesForEarlyRun` : 2 / 2 / 3 / 4 / 5 selon le palier.
> À rouvrir si l'ATB se révèle illisible à cinq.

---

## 6. Déploiement

Le groupe est une entité unique en exploration (un seul jeton). À l'entrée en combat, cette
entité se **dépiaute** en 4 individus, déployés sur les cases libres autour de la position du
jeton au moment du déclenchement. À la sortie, elle se recompose.

### Placement ennemi

Les ennemis se déploient **selon leur famille de Bestiaire**, sur un rayon de **8 à 20 cases**
autour des alliés — potentiellement n'importe où sur la carte, donc, dès lors que la distance
tombe dans cette fourchette.

Le comportement de placement par famille réutilise les Attitudes et les groupes cohérents déjà
livrés (Phases 11–14 du chantier Bestiaire) : les Veilleurs du Seuil se placent en barrage, les
Copistes en retrait, etc. Le détail par famille est à spécifier à l'implémentation ; le principe
est que **le placement participe à l'identité de la famille**, il n'est pas générique.

---

## 7. Déroulement du tour

**Tour par tour strict, ordonné par la Vitesse individuelle.** Pas d'ATB, pas de phase
« tout un camp puis l'autre » : alliés et ennemis sont mêlés dans un ordre d'initiative unique,
recalculé au début de chaque round et **affiché au joueur**.

Le principe directeur : *la complexité vient des scripts ennemis, des combos de compétences et
du terrain — pas de l'horloge.* L'ordre d'action doit être prévisible d'un coup d'œil.

**Chaque personnage est contrôlé individuellement.** Pas de mouvement de groupe.

### Ce qui ne traverse pas

Le **coût d'investissement** (payer du tempo pour frapper plus fort) et le **momentum** n'ont
pas d'équivalent en tour par tour strict. Ils restent intégralement côté ATB. C'est un choix
assumé : le combat tactique trouve sa profondeur dans le positionnement, la portée, les zones
d'effet et les scripts ennemis.

---

## 8. Économie d'action

Chaque tour comporte **deux actions distinctes : se déplacer, puis agir.**

- Les deux sont **indépendantes** : renoncer à l'une ne pénalise ni ne bonifie l'autre.
- Les quatre combinaisons sont légales : se déplacer seulement, agir seulement, faire les deux,
  ne rien faire.
- L'ordre est **déplacement puis action**. Agir puis se déplacer n'est pas prévu en v2.

> Ceci **remplace** la « fusion déplacement + action en une seule interaction » de la v1 (§5),
> qui décrivait un modèle différent.

---

## 9. Mouvement

- **Portée de déplacement** = base fixe par archétype (Bruiser/Guard plus lents, Skirmisher plus
  mobile) **+ bonus/malus dérivé de la Vitesse**. Coefficients à calibrer à l'implémentation.
- Explicitement **pas** une moyenne de toutes les stats (rejeté : pénalise la spécialisation
  sans lien avec le rôle).
- L'**élévation coûte du déplacement** — même principe qu'en exploration, où c'est déjà livré.
- Le pathfinding réutilise `RoomGrid.FindPath` (livré, chantier Terrain P1).
- Modificateurs de portée via équipement : **hors scope v2**, sans que le modèle l'empêche.

---

## 10. Compétences — portée et zone d'effet

- **Portée définie par compétence** (la Catégorie Physique/Magique peut fournir un défaut
  d'authoring, sans être la source de vérité).
- **Zone d'effet** : forme fixe par compétence (croix, losange…), taille variable. Les formes
  alternatives (ligne, cône, zone libre) sont **hors scope v2**.
- **Compétences « toute la carte »** : catégorie à part, strictement symétrique — touchent tout
  le monde sur la grille, alliés compris, que l'effet soit offensif ou bénéfique. C'est un vrai
  levier de risque, assumé comme tel.
- **Réinterprétation spatiale des modes de ciblage existants** : `AllEnemies` devient « tous les
  ennemis dans la zone d'effet », pas « tous les ennemis de la carte » — sauf pour les
  compétences explicitement « toute la carte ».
- La ligne de vue est calculée par la logique déjà livrée en exploration (`RevealAround`,
  chantier Terrain P4), réutilisée telle quelle.

---

## 11. Terrain et élévation

- **Cases infranchissables et trous** : bloquent le déplacement, et comptent dans les calculs de
  ligne de vue et de portée.
- **Élévation** — pleinement mécanique :
  - coûte du déplacement pour être gravie ;
  - bloque la ligne de vue (logique d'exploration réutilisée) ;
  - **depuis une position surélevée, les attaques à distance gagnent +5 % de précision et
    +5 % de dégâts** (`// BALANCE KNOB`).
- Le bonus de hauteur ne s'applique **qu'aux attaques à distance** — pas au corps-à-corps.
- Catalogue élargi d'effets de terrain : **hors scope v2**.

---

## 12. Paliers de risque

Le palier (Calme → Fatal, livré) influence aussi la grille de combat : surface utile, sévérité
du terrain, distance de déploiement, et nombre d'ennemis — en plus des multiplicateurs déjà en
place (stats, butin, réputation, Éclats).

La mise « provoquer le destin » (livrée) est inchangée dans son fonctionnement ; ses effets se
répercutent simplement aussi sur la grille.

Valeurs en tables `// BALANCE KNOB`, suivant la convention en place.

---

## 13. IA ennemie

- **Ciblage** : réutilise les Attitudes et les groupes cohérents du Bestiaire (poids combinés :
  faiblesse de type/catégorie **et** préférence d'archétype). La cible est choisie **sans tenir
  compte de la portée** ; le mouvement exécute ensuite le déplacement nécessaire.
- **Contrainte de portée sur le choix de cible** : hors scope v2.
- **Cohésion de groupe** : les ennemis d'un même groupe cherchent à rester à portée de
  compétence de leurs alliés — décisif pour les archétypes de soutien.
- Le comportement de déplacement, comme le placement initial (§6), est **spécifique à la
  famille**.

---

## 14. Rangs et Lois du Palais

Les rangs `Front`/`Back` restent **exclusivement ATB**. Sur la grille, la position réelle les
remplace : être devant, c'est se tenir devant. Aucune réécriture du système de rangs.

En revanche, les Lois qui manipulent le rang doivent recevoir une **traduction spatiale**. La
règle de conversion :

> Une Loi qui déplace une cible d'un rang à l'autre devient, en mode tactique, un déplacement
> forcé de **X cases** — soit en éloignement, soit en rapprochement vers le lanceur, selon
> l'intention de la Loi.

Exemple : le Vent de la Falaise, qui pousse une cible du rang avant vers l'arrière, devient un
recul forcé de X cases.

**Un inventaire des ~37 Lois livrées est requis** pour identifier celles concernées et fixer
leur X. Ce n'est pas un chantier lourd, mais il ne doit pas être découvert en cours de route.

---

## 15. Victoire, défaite, fuite

Inchangées, identiques à l'ATB : victoire quand tous les ennemis sont défaits, défaite quand
tous les alliés le sont. Aucune condition nouvelle (évasion, protection d'unité, survie N tours)
n'est introduite.

---

## 16. Ce qui ne change pas

- L'ATB reste **entièrement fonctionnel**. Aucune régression tolérée — hors le passage à
  4 personnages (§5), qui est un changement voulu et transverse.
- Aucune récompense, progression, Loi, monnaie, réputation ni contenu catalogue n'est affecté
  par le mode de combat.

---

## 17. Hors scope v2

- Contrainte de portée dans le choix de cible de l'IA.
- Formes de zone d'effet alternatives (ligne, cône, zone libre).
- Modificateurs de portée de compétence ou de déplacement via équipement.
- Catalogue élargi d'effets de terrain.
- Nouvelles conditions de victoire ou de fuite.
- Changement de mode de combat en cours de run.
- Agir avant de se déplacer.
- Mouvement de groupe (rejeté définitivement, pas reporté).

---

## 18. Points à trancher à l'implémentation

- Coefficients du bonus de mouvement dérivé de la Vitesse.
- Tables `BALANCE KNOB` : surface et sévérité de terrain par palier, nombre d'ennemis par palier.
- Rayon exact et règles de collision du déploiement allié.
- Comportement de placement détaillé, famille par famille (§6).
- Valeur de X par Loi à effet de rang (§14).
- Répartition des poids de ciblage IA (type vs archétype).

---

## 19. Ordre de chantier recommandé

1. ~~**Extraction du noyau de résolution partagé** hors de `Combat.cs` / `Combatant.cs`.~~
   ✅ **Livré.** `ICombatContext` (domaine) porte les 14 membres que la résolution consomme, plus
   deux crochets d'ordonnancement (`InterruptAction`, `AwardTempoMomentum`) qu'un moteur à ordre de
   tour fixe neutralise. `Combat` l'implémente ; `CombatSkillEffectResolver` ne connaît plus que le
   contrat. Aucun changement de comportement côté ATB.
2. ~~Passage à 4 personnages (transverse ATB + tactique) et extension de l'échelle d'ennemis.~~
   ✅ **Livré.** `Run.MaxPartySize = 4` appliqué à la composition d'équipe au lancement (elle ne
   l'était nulle part : la run embarquait tout le roster). Plafond d'ennemis porté à 5 dans
   `EncounterCompositionPolicy`, atteignable au seul palier Fatal.
3. Agrégat `TacticalCombat` : initiative, économie d'action, état de grille.
4. Vidage/restauration des nœuds au chargement et à la sortie du combat.
5. Déploiement : allié autour du jeton, ennemi par famille sur rayon 8–20.
6. Portée, zones d'effet, réinterprétation spatiale des modes de ciblage.
7. Élévation en combat (coût, ligne de vue, bonus distance).
8. IA de déplacement et cohésion de groupe.
9. Traduction spatiale des Lois à effet de rang.
10. Garde-fou de surface à la génération.
11. Frontend : rendu des unités, ordre d'initiative, prévisualisation de portée et de zone,
    caméra déroulante.
