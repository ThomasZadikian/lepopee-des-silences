# SFD canonique — Combat T-RPG

Statut : **référence d'implémentation**

Ce document remplace, en cas de divergence, les règles de
`docs/design/sfd-combat-trpg-v2.md` et les déductions historiques du runtime.
Il consolide les arbitrages de conception validés pour l'intégration finale du
handoff Claude Design.

## 1. Principes structurants

- Le T-RPG est l'unique système de combat.
- L'ATB est supprimé intégralement du runtime, du domaine, des handlers, des
  DTO, de la persistance et des colonnes actives.
- Les migrations EF historiques restent dans l'historique.
- La statistique `Recovery` est supprimée avec l'ATB.
- Les sauvegardes contenant un combat ATB actif sont supprimées.
- Une sauvegarde T-RPG restaure exactement l'état du combat et son unité active.
- Tous les tirages sont déterministes : une même action depuis un même état
  reproduit les mêmes résultats.

## 2. Composition et déploiement

### 2.0 Déclenchement depuis l'exploration

- Toute rencontre ordinaire (`Combat`, `Rare`, `Elite`, `RoomBoss`,
  `FinalBoss`) est représentée par un ennemi mobile dans la salle et démarre au
  contact, conformément à `LEDS-SFD-EXP-001`.
- À trois cases ou moins en distance de Manhattan, l'ennemi poursuit le joueur.
- La case de contact devient la case d'événement utilisée comme centre de
  déploiement tactique.
- Les combats produits par une règle locale, un dialogue, une zone ou une
  séquence scénarisée restent déclenchés par ce mécanisme authored. Ils ne
  créent pas d'ennemi mobile ; cette exception couvre notamment les protocoles
  du Hall d'entrée.

### 2.1 Équipe

- L'équipe comporte au maximum quatre alliés.
- Sa composition est choisie avant la run depuis une future interface dédiée
  de l'écran titre.
- Aucun allié n'occupe la case d'événement.
- Les alliés sont placés en étoile, sur les quatre cases cardinales autour de
  la case d'exploration ayant déclenché l'événement.
- Si une branche est bloquée ou hors de la salle, la première case libre est
  recherchée par anneaux croissants.

### 2.2 Ennemis

- Six ennemis initiaux au maximum.
- Les invocations peuvent dépasser temporairement cette limite sans plafond
  supplémentaire.
- Le placement initial suit une formation propre à la famille, puis est ajusté
  selon le rôle de chaque ennemi.
- Un ennemi sans comportement authored utilise l'IA générique.

## 3. Initiative et activation

- L'ordre unique mélange alliés et ennemis.
- La Vitesse détermine l'ordre d'initiative.
- L'ordre n'est recalculé que lorsqu'une Vitesse change.
- Le recalcul est immédiat et reconstruit tout l'ordre.
- Après recalcul, le curseur revient au premier combattant du nouvel ordre.
- Un combattant ayant déjà agi peut rejouer.
- Le nombre d'activations supplémentaires n'est pas plafonné.
- Chaque activation supplémentaire compte comme un tour du porteur pour les
  statuts et les temps de recharge.
- Un bonus de Vitesse expirant à la fin d'une activation déclenche
  immédiatement le recalcul.

## 4. Économie d'action

Chaque activation fournit :

- une action de déplacement ;
- une action de combat.

Les deux actions sont indépendantes et peuvent être utilisées dans n'importe
quel ordre. Le joueur peut terminer son tour à tout moment.

### 4.1 Déplacement

- Une seule action de déplacement est autorisée par activation.
- Le combattant peut dépenser tout ou partie de son budget.
- Une fois le déplacement confirmé, le reliquat est perdu.
- Le déplacement ne peut pas être fractionné autour de l'action de combat.
- Le trajet est prévisualisé selon la position de la souris.
- Un clic valide et exécute immédiatement le trajet.
- Un déplacement validé est irréversible.

### 4.2 Action

Une action permet notamment :

- une attaque de base ;
- une compétence ;
- un objet ;
- une action spéciale explicitement authorée.

L'utilisation d'un objet ne consomme pas le déplacement.

## 5. Statistique Movement

- `Movement` est une statistique dédiée, indépendante de la Vitesse.
- Formule :

  `Movement = base + bonus équipement + modificateurs de statuts`

- Les modificateurs sont des entiers additifs.
- La valeur finale est bornée à un minimum de `1`.
- Chaque combattant doit déclarer sa valeur de base dans le catalogue.
- Le fallback temporaire est `4`.

### 5.1 Coûts

- Déplacement orthogonal uniquement.
- Une case plane coûte `1`.
- Une montée coûte `2`, quelle que soit l'élévation gagnée :
  `1` pour la case et `1` de surcharge fixe.
- Une descente coûte `0`, quelle que soit l'élévation perdue.
- Les diagonales sont interdites.
- Les alliés peuvent être traversés, mais leur case ne peut pas être occupée à
  l'arrivée.
- Les ennemis ne peuvent pas être traversés.
- Les réactions de sortie de zone n'existent que pour les unités possédant une
  compétence dédiée.

## 6. Portée, formes et ligne de vue

### 6.1 Contrat obligatoire

Chaque compétence et chaque objet utilisable en combat doit déclarer :

- la portée ;
- la forme ;
- l'exigence de ligne de vue ;
- le temps de recharge pour une compétence ;
- le registre émotionnel utilisé par le rendu.

Aucun fallback fondé sur l'ancien `TargetingType` n'est autorisé dans le
catalogue final.

### 6.2 Mesure de portée

La portée est tridimensionnelle :

`distance = |Δx| + |Δy| + |Δélévation|`

Chaque niveau d'écart consomme donc un point de portée.

### 6.3 Formes

- `case` : la case centrale uniquement ;
- `croix` : rayon fixe `1` ;
- `losange` : rayon fixe `2` en distance de Manhattan ;
- `carte` : toutes les cases et tous les combattants.

La ligne de vue d'une zone est contrôlée entre le lanceur et la case centrale.
Une fois le centre valide, les unités masquées dans la zone sont affectées.
Une forme `carte` ignore toujours la ligne de vue.

### 6.4 Blocage et interception

- Tous les combattants vivants bloquent la ligne de vue.
- Si un combattant se trouve sur la trajectoire, il intercepte l'effet.
- Un allié peut intercepter et subir un effet hostile.
- Tout effet nécessitant une ligne de vue est interceptable, y compris un soin
  ou un soutien.
- Le jet de précision calculé contre la cible initiale est conservé.
- Un projectile de zone intercepté déclenche sa zone sur la case de
  l'intercepteur.
- L'intercepteur se tourne vers le lanceur.
- La cible initiale ne change pas d'orientation.

### 6.5 Tir ami

- Le ciblage allié hostile est interdit.
- Une interception peut néanmoins blesser un allié.
- Les compétences de forme `carte` affectent les deux camps.

## 7. Hauteur

- Une unité plus haute peut voir toute cible strictement plus basse sans tenir
  compte des obstacles de ligne de vue.
- La ligne de vue normale reste applicable vers les unités de même hauteur ou
  plus hautes.
- Une attaque à distance depuis une position plus haute reçoit :
  - `+5 %` de dégâts ;
  - `+4` points de critique.
- Le bonus s'applique dès que l'attaquant est strictement plus haut.

## 8. Orientation

Les unités possèdent une orientation cardinale.

- Les cases et diagonales avant comptent comme la face.
- Les cases et diagonales arrière comptent comme le dos.
- Les côtés comptent comme les flancs.

| Angle | Précision | Critique | Dégâts |
|---|---:|---:|---:|
| Face | — | — | — |
| Flanc | +10 | +2 points | +5 % |
| Dos | +20 | +4 points | +10 % |

- Après une attaque, la cible se tourne vers l'attaquant, même si l'attaque
  échoue.
- Après une zone, chaque unité affectée se tourne vers le centre de la zone.
- Le lanceur d'une zone se tourne vers la case centrale ciblée.
- Après une action monocible, le lanceur se tourne vers sa dernière cible.
- Sans cible, l'unité se tourne vers l'ennemi visible le plus proche.

## 9. Précision, critique et dégâts

### 9.1 Précision

- Le système existant de précision et d'esquive est conservé.
- Un statut hostile est appliqué automatiquement si l'attaque porteuse touche.

### 9.2 Critique

- La chance de critique conserve la contribution du Focus.
- Les bonus de hauteur et d'orientation s'ajoutent en points de critique.
- Un critique multiplie le résultat par `1,6`.

### 9.3 Formule de base

Physique :

`base = puissance × Attaque / Défense`

Magique :

`base = puissance × AttaqueMagique / DéfenseMagique`

Si la Défense concernée vaut zéro :

- le rapport Attaque/Défense est entièrement ignoré ;
- la base vaut directement `puissance × 1,15`.

### 9.4 Pipeline

1. formule de base ;
2. affinités émotionnelles ;
3. variation déterministe par cible, de `85 %` à `115 %` ;
4. bonus de hauteur ;
5. bonus d'orientation ;
6. critique ;
7. autres amplifications explicites, dont le sacrifice létal ;
8. arrondi classique à l'entier le plus proche ;
9. absorption par la Garde ;
10. application du reliquat à la Vitalité.

Les dégâts finaux peuvent être nuls.

## 10. Mana

- La Mana restante persiste entièrement entre les combats.
- Au début de chaque activation, le porteur récupère :

  `max(1, floor(ManaMax × 0,05))`

- Un repos restaure toute la Mana de l'équipe.
- Les ennemis paient leurs coûts de Mana comme les alliés.

### 10.1 Mana insuffisante

- La Mana disponible est consommée en premier.
- Chaque point manquant coûte `2` points de Vitalité.
- Ce coût contourne la Garde.
- Une confirmation explicite affiche le coût exact.
- Si le coût n'est pas létal, l'action se résout normalement.
- Si le coût est létal :
  - l'action se résout ;
  - dégâts, soins et magnitude des effets reçoivent `+50 %` ;
  - la durée des statuts ne change pas ;
  - le lanceur est éliminé après résolution.
- Si le dernier allié et le dernier ennemi initial tombent ainsi, la défaite
  du joueur est prioritaire.
- Les ennemis utilisent les mêmes règles ; leur décision de sacrifice dépend
  de leur famille et du palier de risque.

## 11. Charge et ultimes

- La Charge est une jauge d'ultime décimale de `0` à `5`.
- Elle revient à zéro à la fin de chaque combat.
- Les compétences ordinaires n'en consomment pas.
- Une compétence ultime consomme de la Charge et de la Mana.
- La Charge manquante ne peut jamais être payée avec de la Vitalité.

### 11.1 Gains directs

- Une action utile produit `0,3`.
- Une élimination directe produit `+0,3` supplémentaire.
- Une zone produit `0,3`, puis `+0,1` par cible utile supplémentaire.
- Le gain d'une action de zone est plafonné à `2`.
- Les effets sur soi-même ne produisent jamais de Charge.

### 11.2 Gains périodiques

Un DoT ou HoT produit à chaque déclenchement :

`0,3 + 0,1 × (nombre de stacks - 1)`

- Le gain total est réparti entre les lanceurs proportionnellement au nombre de
  stacks qu'ils possèdent encore.
- Le bonus d'élimination d'un DoT revient au lanceur de la première stack.
- Les valeurs sont arrondies à une décimale.

## 12. Temps de recharge

- Chaque compétence peut déclarer un temps de recharge en nombre
  d'activations du porteur.
- Le compteur diminue au début de chaque activation.
- Une activation supplémentaire diminue à nouveau les compteurs.
- Une activation neutralisée par un statut diminue aussi les compteurs.
- Tous les temps de recharge sont remis à zéro à la fin du combat.
- Les objets n'ont pas de temps de recharge.

## 13. Statuts

- La durée est exprimée en activations du porteur.
- DoT et HoT se déclenchent au début de l'activation.
- Si un DoT élimine le porteur, son activation est immédiatement sautée.
- Une activation neutralisée reste un tour complet :
  - Mana régénérée ;
  - effets périodiques déclenchés ;
  - statuts décrémentés ;
  - temps de recharge décrémentés.
- Tous les statuts temporaires sont supprimés à la fin du combat.

### 13.1 Stacks

- Maximum universel : `5`.
- Les applications identiques forment une pile commune, même si leurs lanceurs
  diffèrent.
- La durée reste celle de la première application.
- Une nouvelle stack augmente la magnitude sans renouveler la durée.
- À cinq stacks, une nouvelle application est ignorée.
- Une dissipation retire une seule stack.

## 14. Attaque de base et armes

L'arme équipée déclare le contrat complet de l'attaque de base :

- puissance ;
- catégorie physique ou magique ;
- portée ;
- forme ;
- ligne de vue.

Sans arme :

- catégorie physique ;
- portée `1` ;
- forme `case`.

## 15. Objets

- Inventaire partagé par l'équipe.
- Chaque objet de combat possède un contrat tactique obligatoire.
- L'objet consomme l'action, jamais le déplacement.
- La quantité est déduite après résolution, même en cas d'échec ou
  d'interception.
- Aucun temps de recharge.

## 16. Déplacements forcés

- Ils ne consomment ni points ni action de déplacement de la cible.
- Une collision arrête la cible sur la dernière case libre.
- Dégâts de collision :

  `5 % de VitalitéMax × cases de poussée restantes`

- Une unité poussée contre un combattant reçoit les dégâts complets.
- Le combattant percuté reçoit la moitié de ces dégâts.
- Une chute inflige :

  `5 % de VitalitéMax × niveaux descendus`

- Collision et chute sont arrondies à l'entier le plus proche.
- La Garde les absorbe avant la Vitalité.
- Une projection dans un trou ou hors de la carte élimine immédiatement
  l'unité.

## 17. Lois du Palais

- Une Loi est spatiale si elle possède une portée ou une forme tactique.
- Sinon, elle est globale.
- Une Loi spatiale utilise la même grammaire que les compétences.
- Sa zone est toujours centrée sur la case d'événement.
- Les cases restent marquées avec la couleur d'accent de la Loi.
- Une unité déjà déployée dans la zone subit son premier effet au début de sa
  première activation.
- Ensuite, l'effet se déclenche :
  - à chaque nouvelle entrée dans la zone ;
  - au début de chaque activation passée dans la zone.
- Une même unité peut déclencher plusieurs entrées pendant une activation.
- Les effets de plusieurs Lois superposées se cumulent.

## 18. IA

- Les comportements authored des familles et boss sont conservés.
- L'IA générique sert de fallback.
- Le placement suit la famille, puis le rôle.
- La stratégie familiale est modulée par le palier de risque :

| Palier | Modulation |
|---|---|
| Calme | prudence, cohésion forte, cible proche, repli à 40 % |
| Tendu | comportement familial standard, repli à 30 % |
| Sombre | agressivité, cibles blessées/support, compétences rares, repli à 20 % |
| Fatal | coordination et focus, compétences rares stratégiques, pas de repli sauf règle familiale |

Le palier module également agressivité, cohésion, priorité des cibles,
compétences rares et sacrifice de Vitalité.

## 19. Palier de risque

Le palier est fixé au lancement du combat et ne change plus.

- Calme : 1–2 ennemis initiaux ;
- Tendu : 3 ;
- Sombre : 4 ;
- Fatal : 5 ou plus.

Overrides :

- Élite : Sombre ;
- Boss : Fatal ;
- Rare : Tendu au minimum.

| Palier | Dégâts ennemis | Précision | Critique | Movement | Vitesse | Récompense | Résistances | Garde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Calme | ×0,90 | −5 | −2 | −1 | −2 | ×0,90 | −5 | ×0,90 |
| Tendu | ×1,00 | 0 | 0 | 0 | 0 | ×1,00 | 0 | ×1,00 |
| Sombre | ×1,10 | +5 | +2 | +1 | +2 | ×1,25 | +5 | ×1,10 |
| Fatal | ×1,20 | +10 | +4 | +2 | +4 | ×1,50 | +10 | ×1,20 |

Le rendu conserve grade, accent et ambiance de Claude Design.

## 20. Incapacité, réanimation et fin de combat

### 20.1 Incapacité

- À zéro Vitalité, le combattant disparaît immédiatement de la grille.
- Un allié hors combat reste indisponible après la victoire jusqu'à un soin ou
  un repos.
- Il est exclu des gains individuels de progression.

### 20.2 Réanimation

- Une compétence ou un objet explicitement prévu peut réanimer.
- L'allié revient sur une case libre adjacente au lanceur.
- Si aucune case adjacente n'est libre, recherche par anneaux croissants.
- La réanimation recalcule immédiatement toute l'initiative.

### 20.3 Victoire et défaite

- Victoire : tous les ennemis présents au lancement du combat sont vaincus.
- Les invocations restantes disparaissent sans récompense.
- La victoire est évaluée à la fin du tour actif.
- Défaite : tous les alliés sont à zéro Vitalité.
- La défaite est évaluée à la fin du tour actif.
- Une défaite termine définitivement la run.

### 20.4 Après la victoire

- La Vitalité restante persiste.
- La Garde revient à sa valeur de base.
- La Mana restante persiste.
- La Charge revient à zéro.
- Les temps de recharge reviennent à zéro.
- Tous les statuts temporaires disparaissent.

## 21. Fuite par percée

- Disponible uniquement dans les combats normaux.
- Une case de sortie est placée sur un bord derrière les lignes ennemies, de
  préférence au bord extrême de la carte.
- Si la case idéale est inaccessible, la case de bord accessible la plus proche
  est utilisée.
- Dès qu'un allié vivant atteint la sortie, toute l'équipe est évacuée.
- La fuite est immédiate à l'arrivée sur la case.
- L'événement est résolu définitivement.
- Aucune récompense n'est accordée.

## 22. Terrain, nœuds et caméra

- Aucun brouillard de guerre en combat.
- Les nœuds d'exploration se dissolvent en `800 ms`.
- Ils reviennent après le combat en `800 ms`.
- Le champ de bataille réutilise le terrain réel de la salle.
- La caméra :
  - ajuste automatiquement la salle tant que les cases restent lisibles ;
  - impose une taille minimale de case ;
  - active pan et zoom au-delà ;
  - suit doucement l'unité active ;
  - propose un recentrage manuel.

## 23. HUD et rendu

Ordre canonique :

1. fond ;
2. terrain ;
3. grade et désaturation ;
4. unités et surbrillances ;
5. ambiances et effets.

- Le panneau d'action contextuel apparaît près de l'unité active.
- Il est déplaçable par le joueur.
- Initiative et journal de combat restent toujours visibles.
- Avant confirmation d'une compétence, l'interface affiche :
  - précision ;
  - critique ;
  - dégâts ou soins prévus ;
  - coûts ;
  - interception ;
  - affinité ;
  - hauteur ;
  - orientation.
- Les effets visuels sans peinture dédiée sont dérivés du registre émotionnel
  et de la forme tactique, avec exceptions authored.
- Les six boss sans sprite définitif conservent `boss-ombre`.

## 24. Persistance

L'état est sauvegardé après :

- chaque déplacement confirmé ;
- chaque action résolue ;
- chaque fin de tour.

Le snapshot doit contenir au minimum :

- positions et orientations ;
- unité active et ordre d'initiative ;
- actions consommées ;
- Vitalité, Garde, Mana et Charge ;
- statuts, stacks, sources et durées ;
- temps de recharge ;
- invocations et ennemis initiaux ;
- palier fixe ;
- zones de Lois ;
- compteur déterministe des actions et impacts.

## 25. Objets du Palais

### 25.1 Inventaire et équipement

- L'inventaire est partagé par toute l'équipe.
- Sa capacité reste celle du sac de run : `6` entrées distinctes.
- Un consommable identique s'empile jusqu'à `20` unités.
- Tous les effets cumulables sont plafonnés à `5` stacks, sauf limite plus
  restrictive explicitement authorée.
- Chaque combattant peut équiper :
  - un accessoire ;
  - trois reliques.
- Un effet désignant le porteur s'applique au combattant ayant équipé l'objet.
- Une relique globale affecte l'équipe, mais son coût est payé par son porteur.

### 25.2 Contrat tactique par défaut

Tout objet activable en combat utilise, sauf exception explicitement authorée :

- portée `2` ;
- forme `losange` ;
- ligne de vue obligatoire ;
- consommation de l'action uniquement.

Les effets à portée illimitée, de forme `carte` ou visant un allié hors combat
dérogent explicitement à ce contrat. Tout effet soumis à ligne de vue peut être
intercepté conformément aux règles des compétences.

### 25.3 Vitalité maximale

- Une réduction de Vitalité maximale réduit la Vitalité actuelle du même
  montant.
- Une réduction issue d'un objet ne peut jamais faire descendre la Vitalité
  maximale ou actuelle sous `1`.
- Elle ne peut donc pas éliminer directement son porteur.

### 25.4 Ralentissement

Toute ancienne perte de tour ou référence à un gel ATB devient un
ralentissement :

- Vitesse divisée par deux ;
- Movement divisé par deux ;
- portées divisées par deux ;
- arrondi inférieur ;
- minimum `1` pour toute valeur initialement positive ;
- effet visuel de ralentissement.

La durée est exprimée en activations des cibles. Les stacks se cumulent jusqu'à
la limite universelle de cinq.

### 25.5 Grimoires

- Un grimoire apprend une compétence temporaire pour la run.
- Chaque combattant possède deux emplacements de compétence temporaire.
- La Page arrachée peut créer un troisième emplacement.
- Si tous les emplacements sont occupés, le joueur choisit la compétence
  temporaire remplacée.
- Une compétence temporaire peut coexister avec sa version normale.
- Une compétence de forme `carte` affecte toujours les deux camps, quel que soit
  son effet.

### 25.6 Météo

- Les météos du compendium s'ajoutent aux climats existants.
- Une seule météo est active par salle.
- Une nouvelle météo remplace la précédente.
- Les salles sont indépendantes : aucune météo ne persiste naturellement.
- Un objet peut explicitement imposer une météo sur plusieurs salles.
- L'utilisation d'un instrument est interdite dans une salle
  `weather_immune`.
- Une salle immunisée consomme néanmoins une salle de durée d'un effet déjà
  actif.

Nouveaux modificateurs :

| Météo | Effet |
|---|---|
| Accalmie | Neutre |
| Pluie violacée | +25 % dégâts périodiques |
| Brume | −25 % Focus |
| Orage | +15 % dégâts magiques |
| Pluie de cendres | +25 % dégâts de feu, −25 % soins reçus |

### 25.7 Butin sur la grille

- Les objets sont tirés au moment de la mort de l'ennemi.
- Ils apparaissent sur sa dernière case libre.
- Une élimination dans un trou ou hors carte ne produit aucun objet.
- Les invocations utilisent intégralement leur table de butin.
- Le butin ne bloque ni déplacement ni ligne de vue.
- Traverser sa case, volontairement ou par déplacement forcé, déclenche le
  ramassage.
- Le ramassage ne consomme aucune action supplémentaire.
- Une pile récupère tous ses objets en une fois.
- Si l'inventaire est plein, le joueur choisit les objets conservés et peut
  détruire un objet existant.
- Cette décision interrompt la résolution en cours.
- Un objet ramassé est immédiatement persistant pour la run.
- Les objets non ramassés à la victoire sont perdus.
- Une fuite conserve les objets déjà ramassés.
- Éclats et points de compétence sont crédités immédiatement, sans objet au sol.
- Tous les membres, même hors combat, reçoivent les points de compétence.

## 26. Arbitrages d'objets structurants

- `item.aiguille-arret` ralentit tous les ennemis pendant deux de leurs
  activations ; forme carte, sans ligne de vue, une fois par combat.
- `item.tapis-poche` applique −2 Vitesse jusqu'à la fin de la première
  activation de chaque ennemi concerné.
- `item.cahier-noir` résout ses dégâts après quatre activations globales ; ils
  ignorent la Défense mais passent par la Garde.
- `item.cornes-ivoire` ne se déclenche que sur une attaque de mêlée réussie,
  annule l'impact et renvoie 50 % des dégâts sans reproduire les statuts.
- Les dégâts renvoyés produisent de la Charge et attribuent l'élimination au
  porteur.
- `item.iris-amethyste` laisse l'IA choisir déplacement, compétence et cible
  contre son propre camp.
- `item.diapason-audela` empêche toute réanimation du porteur, déclenche
  immédiatement son sort signature sans aucun coût ni recharge et choisit
  automatiquement la cible pertinente la plus proche.
- Les mentions historiques de PP sont normalisées en Mana.
