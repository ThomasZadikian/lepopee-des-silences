# L’épopée des silences — Suivi technique alpha 0.1.0 à 0.1.5

Périmètre : Game Engine backend + Game Client frontend  
Statut : consolidation après stabilisation RoomMap / MapNode / combat progression

---

## 1. Synthèse exécutive

Les versions `0.1.0` à `0.1.5` ont stabilisé une partie structurante de la v2 : la RoomMap jouable.

Le projet est passé d’un modèle trop complexe, basé sur une séparation ambiguë entre `Node` et `NodeEvent`, vers un modèle plus lisible :

```text
Room
└── MapNode[]
```

Dans ce modèle, un `MapNode` représente directement :

- un choix visible ;
- un événement de carte ;
- un point cliquable ;
- un élément de progression ;
- une résolution gameplay possible.

Cette décision réduit la complexité front/back et rend la carte plus exploitable pour la suite du gameplay.

---

## 2. Décisions structurantes prises

### 2.1 Abandon du modèle gameplay Node + NodeEvent

Le modèle précédent :

```text
Room
└── Node
    └── NodeEvent[]
```

a été considéré comme trop lourd pour la vision produit actuelle.

Problèmes observés :

- double niveau de vérité entre `Node` et `NodeEvent` ;
- difficulté à savoir ce que le joueur choisit réellement ;
- projection frontend complexe ;
- risques d’incohérence entre graphe technique et graphe visible ;
- complexité excessive pour la génération, l’affichage et la progression.

Décision :

```text
MapNode = unité visible, cliquable et résolvable.
```

---

### 2.2 Template de RoomMap par défaut

Le template de base retenu est :

```text
[2, 3, 4, 3, 4, 3, 2, 1]
```

Ce qui donne :

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

Ce modèle est considéré comme le template standard actuel, suffisamment lisible pour une première boucle jouable.

---

### 2.3 Structure fixe, architecture modulable

Le template `[2, 3, 4, 3, 4, 3, 2, 1]` est fixe dans le gameplay actuel, mais ne doit pas être codé en dur partout.

La génération doit rester compatible avec une approche template-driven :

```text
RoomMapLayoutTemplate
- Key
- Version
- RoomType
- RowNodeCounts
- InitialRowIndex
- BossRowIndex
```

Objectif futur :

- pouvoir modifier le nombre de profondeurs ;
- pouvoir modifier le nombre de nodes par profondeur ;
- pouvoir varier selon le RoomType ;
- pouvoir varier selon la version du générateur ;
- pouvoir varier selon certaines Lois du Palais ;
- garder la reproductibilité par seed + version.

---

## 3. Réflexion ouverte — vers un modèle semi-dynamique futur

Une question reste ouverte pour une version ultérieure : faut-il conserver un template strictement fixe ou introduire un modèle semi-dynamique ?

### 3.1 Problème du full random

Intégrer de l’aléatoire libre dans le nombre de nodes pose plusieurs risques :

- complexité accrue dans le backend ;
- difficulté à garantir la convergence vers le boss ;
- difficulté à éviter les branches mortes ;
- difficulté à garder une UI lisible ;
- tests plus complexes ;
- risque de générer des cartes trop pauvres ou trop denses ;
- difficulté à maintenir une progression équilibrée.

Le full random n’est donc pas recommandé à court terme.

### 3.2 Modèle semi-dynamique envisagé

Une piste plus saine serait un modèle semi-dynamique :

```text
- toujours 2 nodes au départ ;
- toujours 3 nodes pré-boss ;
- toujours 1 boss final ;
- entre les deux, chaque profondeur contient entre 3 et 5 événements.
```

Exemple de forme possible :

```text
Row 0 : 2
Row 1 : 3 à 5
Row 2 : 3 à 5
Row 3 : 3 à 5
Row 4 : 3 à 5
Row 5 : 3 à 5
Row 6 : 3 pré-boss
Row 7 : 1 boss
```

Cette approche offrirait un compromis :

- structure globale stable ;
- début lisible ;
- fin contrôlée ;
- plus de variation au centre ;
- tests encore raisonnables ;
- frontend toujours prévisible ;
- génération plus riche sans devenir chaotique.

### 3.3 Recommandation

Ne pas intégrer cette logique tout de suite.

Pour les versions `0.1.x`, conserver le template fixe actuel.

Le modèle semi-dynamique pourra être envisagé plus tard, par exemple en `0.2.x`, une fois que :

- le flow combat → reward → progression est stable ;
- les rewards sont mieux branchées ;
- les Lois du Palais commencent à influencer la run ;
- les types de rooms sont différenciés ;
- le frontend sait mieux afficher les variations.

Décision provisoire :

```text
0.1.x : template fixe [2,3,4,3,4,3,2,1]
0.2.x+ : étude d’un template semi-dynamique borné
```

---

## 4. Suivi par version

---

# Backend Game Engine 0.1.0

## Objectif

Stabiliser la première boucle jouable autour de la RoomMap, des nodes et de la progression de base.

## État fonctionnel

La version `0.1.0` correspond au socle initial de la boucle :

```text
StartRun
→ génération Room
→ affichage nodes
→ sélection node
→ résolution event
→ progression
```

## Points structurants

- premières API de run exposées ;
- génération d’une Room initiale ;
- nodes visibles côté front ;
- sélection et résolution possibles ;
- première base de progression dans une Room ;
- structure encore en transition entre les anciens concepts `Node` / `NodeEvent`.

## Limites identifiées

- modèle mental encore ambigu entre node visible et event interne ;
- carte trop difficile à enrichir proprement ;
- risque de complexité front si les events deviennent eux-mêmes visibles ;
- nécessité de simplifier le modèle avant d’aller plus loin.

---

# Frontend Game Client 0.1.0

## Objectif

Créer une première interface jouable permettant de tester manuellement les runs.

## État fonctionnel

- écran principal de run ;
- affichage de la RoomMap ;
- affichage des nodes ;
- panneau de détail node ;
- interaction de sélection ;
- affichage des lois du palais ;
- intégration des phases Map / Combat / Reward / EventOutcome.

## Limites

- design encore prototype ;
- carte peu lisible ;
- placeholders présents ;
- comportement de sélection encore dépendant de fallbacks ;
- logique de progression parfois portée par l’UI.

---

# Backend Game Engine 0.1.1

## Objectif

Stabiliser la RoomMap autour du nouveau modèle `MapNode`.

## Décisions

- simplification du modèle de carte ;
- adoption du template `[2,3,4,3,4,3,2,1]` ;
- réduction de la complexité `NodeEvent` ;
- convergence obligatoire vers le boss ;
- utilisation de `row` et `lane` pour structurer la carte ;
- parenté directe via `parentNodeIds`.

## Résultat

La RoomMap devient plus claire :

```text
Room
└── MapNode[]
```

Chaque node porte directement :

- son type ;
- sa position ;
- ses parents ;
- son état ;
- son risque ;
- son profil de récompense ;
- son statut boss / initial.

---

# Frontend Game Client 0.1.1

## Objectif

Adapter la carte au modèle MapNode et corriger les comportements de sélection.

## Modifications principales

### Correction de `selectedNode`

Les anciens fallbacks ont été supprimés :

- plus de fallback vers `resolvedAtCurrentDepth` ;
- plus de fallback vers `lastResolved`.

Nouveau comportement :

```text
selectedNode retourne null si aucun node n’est prévisualisé ou réellement Selected.
```

Conséquence :

- le `NodeDetailPanel` affiche son état vide ;
- il ne réaffiche plus un bouton de progression sur un node déjà résolu ;
- le frontend ne simule plus une sélection inexistante.

### Carte

- affichage basé sur `nodes` ;
- positionnement par `row` / `lane` ;
- chemins dessinés à partir de `parentNodeIds` ;
- atténuation des anciens chemins non choisis ;
- suppression des effets visuels parasites autour des nodes.

### Limite

Cette version reste partielle : le design final n’est pas traité.

---

# Backend Game Engine 0.1.2

## Objectif

Versionner et exposer explicitement les métadonnées de template RoomMap.

## Modifications

`RoomDto` expose désormais :

```text
LayoutTemplateKey
LayoutTemplateVersion
```

## Intérêt

Ces champs permettent :

- d’identifier le template utilisé ;
- de garantir la reproductibilité ;
- d’afficher le template côté client ;
- de préparer les futures évolutions de génération ;
- de différencier les templates par version.

## Tests

Les tests backend couvrent déjà les invariants de RoomMap :

- distribution ;
- boss seul ;
- DAG ;
- localité des connexions ;
- convergence vers le boss ;
- déterminisme ;
- provider de template.

Aucun test manquant identifié à ce stade.

---

# Backend Game Engine 0.1.3

## Objectif

Verrouiller la résolution des MapNodes par type.

## Constat

Le code de production était déjà suffisamment structuré :

```text
INodeEventResolver
NodeEventResolverDispatcher
Resolvers individuels par type
```

Aucune modification de production nécessaire.

## Tests ajoutés

Création de :

```text
Events/NodeEventResolverTests.cs
```

Couverture des types :

- Combat
- Elite
- Item
- Rare
- Rest
- Law
- Merchant
- Npc
- RoomBoss
- FinalBoss
- Curse

Chaque test vérifie :

- NodeId ;
- PrimaryEventType ;
- ResolutionKind ;
- RiskLevel ;
- RewardProfile ;
- Title ;
- Description ;
- RequiresPlayerChoice.

Tests spécifiques :

- Law expose au moins 2 choices ;
- Merchant expose des choices non vides ;
- Npc expose des NarrativeFragments ;
- RoomBoss contient le nom du BossProfile dans le titre ;
- ResolutionKinds distincts pour Combat / Elite / RoomBoss ;
- EventType correctement enregistré pour chaque resolver.

## Tests handler renforcés

`ResolveCurrentEventCommandHandlerTests.cs` augmenté avec :

- échec si selected node déjà résolu ;
- vérification que la topologie RoomMap n’est pas modifiée ;
- cohérence de progression après résolution.

## Résultat

La résolution des nodes par type devient un contrat testé.

---

# Backend Game Engine 0.1.4

## Objectif

Clarifier Rare comme combat rare et préparer les récompenses par tier de combat.

## Décision gameplay

`Rare` n’est pas une récompense directe.

```text
Rare = combat contre un monstre rare.
```

La récompense rare arrive après victoire du combat rare.

## Modifications production

### RareEventContentResolutionStrategy.cs

- retourne désormais `ResolvedRareCombatEventContent` ;
- expose `EnemyTemplateKey = enemy-rare-v1` ;
- utilise le `RiskLevel` du contexte ;
- suit le même pattern que `CombatEventContentResolutionStrategy`.

### ResolveCurrentEventCommandHandler.cs

- `isCombat` inclut désormais `RareCombatStarted` ;
- switch enrichi pour gérer `ResolvedRareCombatEventContent`.

### RewardOfferFactory.cs

Récompenses post-combat devenues tier-aware :

```text
Combat normal → memory_fragment:common
Rare          → memory_fragment:rare + stat +5
Elite         → memory_fragment:elite + stat defense
Boss          → memory_fragment:boss + heal majeur
```

### SubmitCombatActionCommandHandler.cs

- suppression de l’heuristique fragile `TemplateKey.Contains("boss")` ;
- remplacement par lookup du `combatNode.EventType` ;
- `riskLevel` lu depuis le node ;
- fallback à 25 si nécessaire.

## Tests

### NodeEventResolverTests.cs

- Rare attend désormais `RareCombatStarted`.
- Non-régressions :
  - ItemNode_ShouldNotBeTreatedAsRareCombat
  - RareNode_ShouldNotReturnDirectItemReward
  - EliteNode_ShouldRemainCombatType
  - RoomBossNode_ShouldRemainBossType

### RewardOfferFactoryTests.cs

Tests tier :

- CombatOutcome_ShouldUseNormalRewardProfile
- RareCombatOutcome_ShouldUseRareRewardProfile
- EliteCombatOutcome_ShouldUseEliteRewardProfile
- RoomBossOutcome_ShouldUseBossRewardProfile

Chaque test vérifie notamment le `PayloadKey`.

## Résultat

Le modèle combat est clarifié :

```text
Combat = normal
Rare = combat rare
Elite = combat élite
RoomBoss = combat boss
FinalBoss = combat final
```

---

# Backend Game Engine 0.1.5

## Objectif

Corriger et verrouiller le flow post-combat :

```text
combat terminé
→ reward éventuelle
→ progression possible
```

## Bug constaté

Après la correction frontend de `selectedNode`, le flow post-combat ne pouvait plus s’appuyer sur un ancien node résolu sélectionné artificiellement.

Conséquence :

- le combat se terminait ;
- le node était résolu ;
- mais la progression vers les nodes suivants pouvait rester bloquée.

## Correction frontend

Dans `runStore.ts`, ajout de :

```text
progressRunInlineIfReady()
```

Appelée en fin de :

- `selectReward`
- `handleCombatCompleted`

Logique :

```text
si run active
et pas RoomResolved
et pas de reward pendante
alors appeler runApi.progressRun inline
```

Le guard :

```text
status === RoomResolved
```

protège le cas boss vaincu, qui doit passer par `MoveToNextRoom` et non par `ProgressRun`.

## Tests backend ajoutés

### SubmitCombatActionCommandHandlerTests

Six tests ajoutés :

- CompleteCombat_ShouldClearActiveCombatId
- CompleteCombat_ShouldCreatePendingRewardOffer
- CompleteCombat_ShouldKeepCombatNodeResolved
- CompleteRareCombat_ShouldCreateRareRewardOffer
- CompleteEliteCombat_ShouldCreateEliteRewardOffer
- CompleteBossCombat_ShouldCreateBossRewardOfferAndSetRoomToRoomResolved

Les tests capturent le `RewardOffer` via Moq callback pour vérifier la source.

### ProgressRunCommandHandlerTests

Nouveau fichier.

Tests :

- ProgressRun_ShouldAllowProgressionAfterResolvedCombatNode
- guard active combat
- guard pending reward
- guard no resolved node

### SelectRewardCommandHandlerTests

Ajout :

- SelectReward_ShouldClearPendingRewardAndAllowProgression

Vérifie :

- `HasPendingRewardOffer = false`
- `Status = Active`
- progression possible ensuite.

## Résultat

La progression post-combat ne dépend plus de `selectedNode`.

Le flow devient :

```text
Combat terminé
→ activeCombatId cleared
→ reward créée si nécessaire
→ sélection reward
→ reward cleared
→ run active
→ progressRun possible
→ nodes suivants disponibles
```

---

## 5. État actuel consolidé

### Backend

État recommandé :

```text
Game Engine backend : 0.1.5
```

Capacités stabilisées :

- RoomMap MapNode ;
- template versionné ;
- invariants de génération testés ;
- résolution des nodes par type testée ;
- Rare classé comme combat rare ;
- RewardOffer tier-aware ;
- progression post-combat corrigée.

### Frontend

État recommandé :

```text
Game Client frontend : 0.1.1
```

Capacités stabilisées :

- carte basée sur MapNodes ;
- affichage des liens parent → enfant ;
- correction selectedNode ;
- affichage template/version/progression ;
- reprise post-combat via `progressRunInlineIfReady`.

---

## 6. Prochaine étape recommandée

La prochaine étape logique est :

```text
Backend 0.1.6 — verrouiller MoveToNextRoom / RoomResolved / transition boss
```

Objectif :

```text
RoomBoss terminé
→ Reward boss éventuelle
→ RoomResolved
→ MoveToNextRoom
→ nouvelle Room générée
→ nouveaux nodes initiaux disponibles
```

À tester :

- boss terminé met la room en RoomResolved ;
- reward boss sélectionnée ne lance pas ProgressRun ;
- MoveToNextRoom est autorisé après RoomResolved ;
- MoveToNextRoom génère une nouvelle Room valide ;
- nouvelle Room expose deux nodes initiaux disponibles ;
- activeCombatId et pendingReward sont nettoyés ;
- currentDepth est incrémenté ;
- les anciennes rooms restent consultables ;
- la run ne dépasse pas la profondeur maximale.

---

## 7. Points de vigilance

### 7.1 Ne pas refaire la RoomMap maintenant

Le modèle MapNode fonctionne.

À éviter à court terme :

- réintroduire `NodeEvent` comme couche gameplay ;
- rendre le nombre de nodes aléatoire trop tôt ;
- reconstruire des chemins complets côté frontend ;
- déplacer la logique de validité métier dans Vue.

### 7.2 Garder le frontend simple

Le front doit rester un client d’intentions.

Il peut améliorer l’affichage, mais il ne doit pas décider :

- si un chemin est valide ;
- si une reward est due ;
- si un boss est résolu ;
- si une room peut avancer.

### 7.3 Différer le semi-dynamique

La génération semi-dynamique est une bonne piste, mais pas pour la prochaine PR.

À garder pour plus tard :

```text
2 nodes initiaux fixes
3 nodes pré-boss fixes
1 boss final fixe
3 à 5 nodes par row intermédiaire
```

Cette évolution doit être template-driven et testée, pas improvisée.

---

## 8. Commandes utiles

### Build backend

```bash
dotnet build services/game-engine/Leds.GameEngine.slnx
```

### Tests backend

```bash
dotnet test services/game-engine/Leds.GameEngine.slnx
```

### Lancement API local

```bash
dotnet run --project services/game-engine/src/Leds.GameEngine.Api
```

### Commit type documentation

```bash
git add docs/game-engine/alpha-0.1.0-to-0.1.5-follow-up.md
git commit -m "docs(game-engine): document alpha room map progression"
```

---

## 9. Conclusion

La séquence `0.1.0 → 0.1.5` a transformé une carte encore instable en fondation gameplay cohérente.

Le projet dispose maintenant :

- d’une structure de carte lisible ;
- d’un modèle MapNode plus simple ;
- de resolvers testés ;
- de combats classifiés ;
- de rewards post-combat tier-aware ;
- d’un flow post-combat fonctionnel.

La prochaine priorité n’est plus la carte elle-même, mais la transition inter-room et la consolidation de la boucle complète de run.
