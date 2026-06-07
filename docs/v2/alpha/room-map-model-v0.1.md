# L’épopée des silences — RoomMap / MapNode Model v0.1

## Statut

Version de cadrage : `room-map-model-v0.1`
Backend cible : `game-engine-0.1.1`
Frontend cible : `game-client-0.1.0`

Ce document formalise la décision de conception retenue pour la génération et l’affichage des cartes de Room dans la v2 de *L’épopée des silences*.

L’objectif est de stabiliser le modèle de carte avant d’étendre la résolution des événements, le combat, les récompenses, les Lois du Palais et l’intégration Catalog.

---

# 1. Décision structurante

Le modèle `Node + NodeEvent` est abandonné comme structure gameplay principale pour la carte.

La carte de Room repose désormais sur un modèle simplifié :

```text
Room
└── MapNode[]
```

Un `MapNode` représente directement :

* un choix visible ;
* un événement de carte ;
* un point cliquable ;
* une résolution possible ;
* une étape de progression vers le boss de Room.

Il n’y a plus de couche gameplay séparée du type :

```text
Node
└── NodeEvent[]
```

La carte ne doit pas nécessiter de projection complexe `Node -> NodeEvent -> eventLayers`.

Le modèle cible est :

```text
RoomMap = graphe de MapNodes directement jouables.
```

---

# 2. Rôle de `MapNode`

Un `MapNode` est l’unité minimale visible et résolvable de la carte.

Il correspond à ce que le joueur voit comme un nœud de carte.

Exemples de types possibles :

* `Combat`
* `Elite`
* `Item`
* `Npc`
* `Rest`
* `Merchant`
* `Law`
* `Curse`
* `Rare`
* `RoomBoss`
* `FinalBoss`

Un `MapNode` doit donc porter directement les informations nécessaires à son affichage, sa sélection et sa résolution.

Structure fonctionnelle attendue :

```text
MapNode
- Id
- Type
- Row
- Lane
- RiskLevel
- RewardProfile
- ParentNodeIds
- State
- IsBoss
- IsInitial
- HasChosenEventOption
```

---

# 3. Rôle de `Room`

Une `Room` représente une carte de progression.

Elle contient directement les `MapNodes`.

Structure fonctionnelle attendue :

```text
Room
- Id
- Depth
- RoomType
- Theme
- State
- CurrentNodeDepth
- MaxNodeDepth
- TotalNodeCount
- BossPreview
- Nodes
- AvailableNodes
- LayoutTemplateKey
- LayoutTemplateVersion
```

Les champs `LayoutTemplateKey` et `LayoutTemplateVersion` sont importants pour garantir la reproductibilité et l’évolution future des seeds.

---

# 4. Template de RoomMap par défaut

Le template de base pour la v2 est :

```text
[2, 3, 4, 3, 4, 3, 2, 1]
```

Cela signifie :

```text
Row 0 : 2 MapNodes initiaux
Row 1 : 3 MapNodes
Row 2 : 4 MapNodes
Row 3 : 3 MapNodes
Row 4 : 4 MapNodes
Row 5 : 3 MapNodes
Row 6 : 2 MapNodes pré-boss
Row 7 : 1 RoomBoss
```

Total :

```text
22 MapNodes
8 rows
```

Ce template est le modèle standard de RoomMap pour la version actuelle.

---

# 5. Modèle fixe mais architecture modulable

Le template `[2, 3, 4, 3, 4, 3, 2, 1]` est fixe pour le modèle de base, mais il ne doit pas être codé en dur de manière dispersée dans le générateur.

Le système doit rester modulable.

À terme, il doit être possible de changer facilement :

* le nombre de rows ;
* le nombre de nodes par row ;
* le template selon le `RoomType` ;
* le template selon la version du générateur ;
* le template selon certaines Lois du Palais ;
* le template selon un contexte de run particulier.

Le générateur doit donc consommer un template versionné.

Concept recommandé :

```text
RoomMapLayoutTemplate
- Key
- Version
- RoomType
- RowNodeCounts
- TotalNodeCount
- InitialRowIndex
- BossRowIndex
```

Exemple :

```text
Key = threshold-default-v1
Version = room-map-layout-1.0.0
RoomType = Threshold
RowNodeCounts = [2, 3, 4, 3, 4, 3, 2, 1]
InitialRowIndex = 0
BossRowIndex = 7
```

Provider recommandé :

```text
IRoomMapLayoutTemplateProvider
- GetTemplate(RoomType roomType, string generatorVersion)
```

---

# 6. Règles de structure obligatoires

Pour le template par défaut, la RoomMap doit respecter les règles suivantes.

## 6.1 Nombre de nodes

```text
La RoomMap contient exactement 22 MapNodes.
```

## 6.2 Nombre de rows

```text
La RoomMap contient exactement 8 rows : 0 à 7.
```

## 6.3 Distribution par row

```text
Row 0 : 2 nodes
Row 1 : 3 nodes
Row 2 : 4 nodes
Row 3 : 3 nodes
Row 4 : 4 nodes
Row 5 : 3 nodes
Row 6 : 2 nodes
Row 7 : 1 node
```

## 6.4 Départ

```text
Row 0 contient exactement 2 nodes initiaux.
```

Ces nodes doivent être les seuls nodes disponibles au début de la Room.

Ils ne doivent avoir aucun parent.

## 6.5 Boss

```text
Row 7 contient exactement 1 node.
Ce node est le RoomBoss.
Le RoomBoss est seul sur la dernière row.
```

Aucun autre node ne doit être placé sur la dernière row.

---

# 7. Règles de graphe obligatoires

La RoomMap est un graphe orienté acyclique.

## 7.1 DAG

```text
Le graphe ne doit contenir aucun cycle.
```

## 7.2 Parenté

Tout node non initial doit avoir au moins un parent.

```text
Row > 0 => ParentNodeIds.Count >= 1
```

## 7.3 Descendance

Tout node non-boss doit avoir au moins un enfant.

```text
IsBoss = false => au moins un enfant
```

## 7.4 Convergence obligatoire

Tous les nodes doivent avoir un chemin valide vers le RoomBoss.

Aucune branche morte n’est autorisée.

```text
Chaque MapNode non-boss doit pouvoir rejoindre le RoomBoss.
```

## 7.5 Atteignabilité depuis le départ

Tous les nodes non initiaux doivent être atteignables depuis au moins un des deux nodes initiaux.

```text
Chaque MapNode doit appartenir au graphe jouable.
```

## 7.6 Profondeur

Les connexions doivent aller uniquement de la row courante vers la row suivante.

```text
parent.Row + 1 = child.Row
```

Les sauts de row sont interdits.

Interdit :

```text
Row 1 -> Row 3
Row 2 -> Row 5
Row 4 -> Row 4
```

## 7.7 Connexions locales

Les connexions doivent rester locales par lane.

Un parent peut connecter un enfant :

```text
lane identique
lane voisine gauche
lane voisine droite
```

Les grandes diagonales sont interdites.

Les connexions `all-to-all` entre deux rows sont interdites.

---

# 8. Variation procédurale

La structure spatiale de base ne varie pas dans le template par défaut.

La variation entre runs vient de :

* la seed ;
* les types de nodes ;
* les connexions locales exactes ;
* les niveaux de risque ;
* les profils de récompense ;
* les Lois du Palais ;
* les contenus Catalog ;
* les ennemis ;
* les objets ;
* les fragments narratifs ;
* les états et conséquences de run.

Autrement dit :

```text
Structure stable.
Contenu variable.
```

Cette décision rend l’interface plus lisible et la génération plus testable.

---

# 9. États attendus des MapNodes

Les états fonctionnels actuellement attendus sont :

```text
Locked
Available
Selected
Resolved
```

Évolution possible :

```text
Abandoned
Skipped
Disabled
```

Mais ces états supplémentaires ne doivent pas être ajoutés tant qu’ils ne sont pas nécessaires côté backend.

Le frontend peut atténuer visuellement certains nodes passés non choisis, mais la vérité métier doit rester côté backend à terme.

---

# 10. Règles d’affichage frontend

Le frontend doit afficher la RoomMap à partir de :

```text
Room.Nodes
```

Il ne doit pas reconstruire un modèle de gameplay alternatif.

Chaque node est placé à partir de :

```text
Row
Lane
```

Les chemins sont dessinés à partir de :

```text
ParentNodeIds
```

Le frontend peut appliquer des styles visuels :

* node disponible ;
* node sélectionné ;
* node résolu ;
* node verrouillé ;
* node boss ;
* node passé non choisi ;
* chemin actif ;
* chemin atténué.

Mais le frontend ne doit pas calculer la validité métier de la RoomMap.

---

# 11. Responsabilité backend

Le backend est responsable de :

* générer la RoomMap ;
* appliquer le template ;
* assigner les types de nodes ;
* assigner les rows et lanes ;
* générer les parentés ;
* garantir la convergence vers le boss ;
* garantir l’absence de cycle ;
* garantir la reproductibilité par seed ;
* garantir la variation entre seeds ;
* exposer un DTO directement exploitable par le frontend.

Le frontend ne doit jamais corriger une RoomMap invalide.

---

# 12. Tests backend obligatoires

Les invariants suivants doivent être couverts par tests.

## Template

```text
GenerateRoom_ShouldUseDefaultRoomMapLayoutTemplate
GenerateRoom_ShouldPersistLayoutTemplateKeyAndVersion
GenerateRoom_ShouldCreateExactlyTwentyTwoMapNodes_WithDefaultTemplate
GenerateRoom_ShouldCreateFixedDefaultRowDistribution
```

## Départ et boss

```text
GenerateRoom_ShouldCreateExactlyTwoInitialNodes_WithDefaultTemplate
GenerateRoom_ShouldPlaceBossAloneOnFinalRow_WithDefaultTemplate
GenerateRoom_ShouldCreateExactlyOneRoomBoss
```

## Graphe

```text
GenerateRoom_ShouldCreateAcyclicMapGraph
GenerateRoom_ShouldOnlyConnectToNextRow
GenerateRoom_ShouldKeepConnectionsLocalByLane
GenerateRoom_ShouldMakeEveryNodeReachBoss
GenerateRoom_ShouldNotCreateDeadBranches
GenerateRoom_ShouldMakeEveryNodeReachableFromInitialNodes
```

## Déterminisme

```text
GenerateRoom_ShouldBeDeterministicForSameSeed
GenerateRoom_ShouldVaryContentForDifferentSeeds
```

## Modularité

```text
RoomMapLayoutTemplateProvider_ShouldReturnDefaultThresholdTemplate
GenerateRoom_ShouldRespectProvidedLayoutTemplate
```

---

# 13. Critères d’acceptation backend

Une Room générée est valide si :

* elle contient exactement 22 MapNodes ;
* elle respecte la distribution `[2, 3, 4, 3, 4, 3, 2, 1]` ;
* elle contient exactement 2 nodes initiaux ;
* elle contient exactement 1 boss ;
* le boss est seul sur la dernière row ;
* tous les nodes non initiaux ont au moins un parent ;
* tous les nodes non-boss ont au moins un enfant ;
* tous les nodes rejoignent le boss ;
* aucun node n’est une branche morte ;
* aucun cycle n’existe ;
* toutes les connexions vont uniquement vers la row suivante ;
* les connexions restent locales par lane ;
* la génération est reproductible par seed ;
* le contenu varie entre seeds différentes.

---

# 14. Critères d’acceptation frontend

Le frontend est valide à ce stade si :

* il affiche tous les nodes de la Room ;
* il place les nodes à partir de `row` et `lane` ;
* il affiche les chemins à partir de `parentNodeIds`;
* il distingue visuellement les nodes disponibles, résolus, sélectionnés, verrouillés et boss ;
* il atténue les nodes passés non choisis ;
* il ne tente pas de reconstruire le graphe métier ;
* il transmet uniquement des intentions au backend.

---

# 15. Ce qu’il ne faut plus faire

À éviter :

```text
Room -> Node -> NodeEvent -> eventLayers
```

À éviter également :

* multiplier les couches de projection ;
* recalculer tous les chemins complets côté frontend ;
* laisser le frontend décider quels chemins sont valides ;
* coder le template `[2,3,4,3,4,3,2,1]` en dur dans plusieurs classes ;
* autoriser des branches mortes ;
* autoriser des sauts de row ;
* autoriser des connexions qui traversent toute la carte ;
* rendre variable le nombre de nodes dans le template de base.

---

# 16. Roadmap immédiate

## Backend 0.1.2

Objectif :

```text
Verrouiller les invariants RoomMap par tests.
```

Travail attendu :

* compléter les tests de génération ;
* tester le template ;
* tester la distribution ;
* tester la convergence boss ;
* tester l’absence de branches mortes ;
* tester la localité des connexions ;
* tester le déterminisme.

## Backend 0.1.3

Objectif :

```text
Stabiliser la résolution des MapNodes par type.
```

Travail attendu :

* résoudre `Combat`;
* résoudre `Elite`;
* résoudre `Item`;
* résoudre `Rest`;
* résoudre `Npc`;
* résoudre `Merchant`;
* résoudre `Law`;
* résoudre `RoomBoss`;
* produire des outcomes propres et testables.

## Frontend 0.1.1

Objectif :

```text
Améliorer le feedback de sélection et de progression.
```

Travail attendu :

* nettoyer les placeholders ;
* améliorer les libellés ;
* afficher les informations du template ;
* afficher seed et generatorVersion ;
* clarifier le panneau de détail ;
* rendre la carte plus lisible sans changer le modèle.

---

# 17. Décision finale

Le modèle RoomMap v0.1 est officiellement :

```text
Room -> MapNode[]
```

avec template par défaut :


```text
[2, 3, 4, 3, 4, 3, 2, 1]
```

Le modèle est :

```text
fixe dans sa structure de base,
modulable dans son architecture,
variable dans son contenu.
```

Cette décision sert de fondation pour les prochaines versions du Game Engine et du Game Client.
