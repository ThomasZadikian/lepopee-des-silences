# L’épopée des silences — Game Engine Service

## Suivi technique alpha 0.0.2 — RoomPlan visible, graphe convergent, progression par chemins et contraintes d’événements

Date : 2026-05-30
Branche : `v2/develop`
Service concerné : `services/game-engine`
Statut : validé par tests unitaires, tests d’intégration et vérifications API manuelles Swagger

---

# 1. Objectif du jalon

Ce jalon vise à stabiliser la base gameplay du Game Engine Service pour la refonte v2 de **L’épopée des silences**.

L’objectif principal était de corriger le modèle de Room pour qu’il corresponde réellement à la vision cible du jeu :

* une Room n’est pas un simple écran de choix ;
* une Room est un plan stratégique complet ;
* une Room contient plusieurs nodes ;
* chaque node peut contenir plusieurs événements ;
* le joueur choisit un chemin ;
* les branches non choisies deviennent inaccessibles ;
* toutes les branches doivent converger vers le boss de room ;
* le boss est le dernier node obligatoire ;
* la progression est serveur-autoritaire.

Ce jalon remplace donc une première version trop simplifiée du modèle par une base plus proche de la finalité.

---

# 2. Contexte avant refactor

Avant ce jalon, le Game Engine Service disposait déjà des fondations suivantes :

* solution Clean Architecture initialisée ;
* séparation Domain / Application / Infrastructure / API ;
* MediatR / CQRS ;
* endpoints de base pour démarrer et consulter une run ;
* modèle initial `Run`, `Room`, `Node` ;
* génération déterministe initiale par seed ;
* repository in-memory temporaire ;
* tests unitaires et tests d’intégration ;
* endpoints :

  * `POST /api/v2/runs`
  * `GET /api/v2/runs/{runId}`
  * `POST /api/v2/runs/{runId}/nodes/{nodeId}/choose`
  * `POST /api/v2/runs/{runId}/current-event/resolve`
  * `POST /api/v2/runs/{runId}/nodes/next`

Cependant, le modèle précédent avait plusieurs limites :

* les nodes suivants étaient générés progressivement ;
* la Room n’était pas représentée comme un plan complet ;
* un node ne portait qu’un seul type d’événement ;
* le boss n’était pas encore intégré comme dernier node obligatoire d’un graphe ;
* les branches n’étaient pas réellement des chemins ;
* la progression ne dépendait pas encore du parent choisi ;
* les contraintes de rareté des événements n’étaient pas appliquées ;
* `Memory`, `Rest`, `Npc`, `Item`, etc. pouvaient apparaître de manière incohérente ;
* la matrice Markov n’était pas encore structurante dans la génération.

Ce refactor a donc été décidé pour éviter d’empiler du développement sur une base métier incorrecte.

---

# 3. Décisions métier actées

## 3.1 Une Room est un plan visible complet

La Room est désormais générée entièrement dès son entrée.

Le joueur connaît dès le début :

* le nombre total de nodes de la Room ;
* les différentes profondeurs de la Room ;
* les nodes disponibles immédiatement ;
* les nodes futurs ;
* le nombre d’événements visibles par node ;
* la nature générale des événements visibles ;
* le boss de room à venir.

Cela permet au joueur de prendre une décision stratégique.

Le joueur ne connaît pas encore :

* les ennemis exacts ;
* les récompenses exactes ;
* les effets numériques exacts ;
* les détails narratifs exacts ;
* les résultats précis des événements ;
* les événements `Memory` dynamiques.

La règle retenue est donc :

> Le joueur voit la structure stratégique, mais pas la résolution exacte.

---

## 3.2 Une Room contient entre 6 et 10 nodes

Une Room doit contenir entre 6 et 10 nodes au total, boss inclus.

Cette règle a été posée pour garantir :

* une durée de Room suffisante ;
* une vraie prise de décision ;
* une carte lisible ;
* une génération contrôlable ;
* une base adaptée au futur frontend.

Invariant métier :

```text
6 <= Room.TotalNodeCount <= 10
```

---

## 3.3 Chaque node contient entre 1 et 4 événements

Un node n’est plus limité à un seul événement.

Un node peut désormais contenir une séquence de 1 à 4 événements visibles.

Exemples :

```text
Node A : Combat
Node B : Combat + Item
Node C : Npc + Item + Combat
Node D : Rest
```

Invariant métier :

```text
1 <= Node.EventCount <= 4
```

Ce changement est important, car il permet de créer des nodes plus intéressants que de simples cases monotype.

---

## 3.4 La progression fonctionne par chemins

Le joueur ne peut progresser que vers les enfants du node choisi.

Ancien comportement :

```text
Résoudre un node
→ débloquer toute la couche suivante
```

Nouveau comportement :

```text
Résoudre un node
→ débloquer uniquement les enfants directs de ce node
```

Un node enfant est atteignable si :

```text
child.ParentNodeIds contient resolvedNode.Id
```

Cette règle transforme la Room en vraie carte de chemins.

---

## 3.5 Les branches non choisies deviennent inaccessibles

Lorsqu’un joueur choisit un node :

* le node choisi passe en `Selected` ;
* les autres nodes de la même profondeur passent en `Locked` ;
* les futurs nodes non atteignables depuis le node choisi passent en `Unreachable` ;
* les futurs nodes encore atteignables restent `Planned`.

Cela permet au frontend d’afficher clairement :

* le chemin choisi ;
* les branches verrouillées ;
* les branches futures encore possibles ;
* les branches définitivement perdues.

---

## 3.6 Toutes les branches convergent vers le boss

La génération d’une Room doit garantir qu’aucune branche ne mène à une impasse.

Chaque node non-boss doit avoir au moins un chemin valide vers le boss de room.

Invariant métier :

```text
Pour chaque node non-boss :
il existe au moins un chemin jusqu’au RoomBossNode.
```

Le graphe de Room doit donc être un graphe orienté acyclique en couches, avec convergence obligatoire vers le boss.

---

## 3.7 Le BossNode est unique et final

Le boss de room est représenté par un node spécifique : le `RoomBossNode`.

Règles actées :

* une Room contient exactement un boss de room ;
* le boss est placé à la dernière profondeur ;
* le boss est le seul node de la dernière profondeur ;
* le boss contient l’événement `RoomBoss` ;
* le boss démarre en `Planned` ;
* il devient `Available` uniquement lorsque le joueur atteint la dernière profondeur ;
* sa résolution termine la Room.

Invariant métier :

```text
FinalLayer.Count == 1
FinalLayer.Single().IsRoomBossNode == true
```

Après résolution du boss :

```text
RoomState = Completed
RunStatus = RoomResolved
```

---

# 4. Contraintes d’événements actées

Les contraintes suivantes sont désormais appliquées à la génération.

## 4.1 Combat

```text
Par node : illimité
Par room : illimité
```

`Combat` est l’événement de fallback principal.

---

## 4.2 Elite

```text
Maximum par room : 1
```

---

## 4.3 Item

```text
Maximum par node : 1
Maximum par room : floor(totalNodeCount / 2)
```

Exemple :

```text
Room avec 10 nodes
→ maximum 5 Item dans la room
```

---

## 4.4 Npc

```text
Maximum par room : 1
```

---

## 4.5 Memory

```text
Interdit dans le plan visible de la Room
```

`Memory` n’est plus généré comme événement planifié dans un node.

Il sera traité plus tard comme un événement dynamique pouvant apparaître lors de la résolution d’un node.

Règle cible :

```text
Memory = événement aléatoire de résolution
Memory != événement visible planifié
```

---

## 4.6 Rest

```text
Maximum par room : 1
Exclusif dans son node
RewardProfile = healing-only
```

`Rest` ne donne pas de récompense classique.

Son rôle est uniquement de permettre au joueur de se soigner.

Exemple valide :

```text
Node : Rest
RewardProfile : healing-only
```

Exemples invalides :

```text
Rest + Combat
Rest + Item
Rest + Npc
Rest + Rest
```

---

## 4.7 Merchant

```text
Maximum par room : 1
```

---

## 4.8 Law

```text
Maximum par room : 1
```

---

## 4.9 Curse

```text
Maximum par room : 1
```

---

## 4.10 Rare

```text
Maximum par room : 3
```

---

## 4.11 RoomBoss

```text
Exactement 1 par room
Uniquement sur le RoomBossNode
```

---

## 4.12 FinalBoss

```text
Interdit dans les rooms standards
Réservé à la room finale / Him’Lit
```

---

# 5. Évolution du modèle Domain

## 5.1 NodeState

L’énumération `NodeState` a évolué pour gérer les branches inaccessibles.

États actuels :

```csharp
public enum NodeState
{
    Planned = 0,
    Available = 1,
    Selected = 2,
    Locked = 3,
    Resolved = 4,
    Unreachable = 5
}
```

Signification :

```text
Planned
→ node visible dans le plan, mais pas encore sélectionnable

Available
→ node sélectionnable par le joueur

Selected
→ node choisi, événement en cours ou prêt à être résolu

Locked
→ node de la même profondeur non choisi

Resolved
→ node choisi et résolu

Unreachable
→ node futur situé sur une branche devenue impossible
```

---

## 5.2 Node

Le modèle `Node` a été profondément refactorisé.

Ancien modèle :

```text
Node
- EventType
- ParentNodeId
```

Nouveau modèle :

```text
Node
- Events
- EventTypes
- EventCount
- ParentNodeIds
- ParentNodeId de compatibilité
- IsRoomBossNode
- State
```

Un node peut désormais :

* contenir plusieurs événements ;
* avoir plusieurs parents ;
* participer à une convergence de branches ;
* être marqué inaccessible ;
* représenter le boss de room.

---

## 5.3 ParentNodeIds

`ParentNodeId` ne suffit plus pour représenter les graphes convergents.

Exemple :

```text
A ─┐
   ├── C
B ─┘
```

Dans ce cas, `C` doit avoir deux parents :

```json
"parentNodeIds": ["A", "B"]
```

Le champ `parentNodeId` est conservé temporairement pour compatibilité et lecture simple, mais le champ cible est désormais :

```text
parentNodeIds
```

---

## 5.4 Room

`Room` est maintenant responsable des invariants du graphe.

Elle vérifie notamment :

* le nombre total de nodes ;
* l’existence d’un boss unique ;
* la position du boss ;
* l’unicité du node final ;
* la continuité des profondeurs ;
* la validité des parents ;
* l’absence de parents sur les nodes initiaux ;
* l’existence d’au moins un parent pour les nodes non-initiaux ;
* l’existence d’au moins un enfant pour chaque node non-boss ;
* l’existence d’un chemin vers le boss pour chaque node non-boss.

La Room n’est donc plus une simple collection de nodes.

Elle est devenue le garant métier de la carte.

---

# 6. Évolution du modèle Application / DTO

## 6.1 NodeDto

`NodeDto` expose désormais :

```text
Id
EventTypes
EventCount
RiskLevel
RewardProfile
State
NodeDepth
ParentNodeId
ParentNodeIds
IsRoomBossNode
```

Ce DTO permet au frontend de dessiner une carte complète en couches et en chemins.

---

## 6.2 NodeLayerDto

Les nodes sont exposés regroupés par profondeur :

```text
NodeLayerDto
- Depth
- Nodes
```

Cela donne une structure directement exploitable pour afficher une carte.

---

## 6.3 RoomBossProfileDto

La Room expose une prévisualisation du boss :

```text
BossId
Name
RoomType
DangerHint
```

Exemple :

```json
{
  "bossId": "threshold-guardian",
  "name": "Gardien du Seuil",
  "roomType": "Threshold",
  "dangerHint": "High"
}
```

---

## 6.4 RoomDto

`RoomDto` expose désormais :

```text
Id
Depth
RoomType
Theme
State
CurrentNodeDepth
MaxNodeDepth
TotalNodeCount
BossPreview
NodeLayers
AvailableNodes
```

Cela donne au client un état complet, lisible et stratégique de la Room.

---

# 7. Évolution du flux API

## 7.1 Démarrer une run

Endpoint :

```http
POST /api/v2/runs
```

Résultat :

* crée une run active ;
* génère une room complète ;
* expose tout le plan visible ;
* place les nodes de profondeur 0 en `Available` ;
* place les nodes futurs en `Planned`.

---

## 7.2 Choisir un node

Endpoint :

```http
POST /api/v2/runs/{runId}/nodes/{nodeId}/choose
```

Effets :

* le node choisi passe en `Selected` ;
* les autres nodes de la profondeur courante passent en `Locked` ;
* les branches futures non atteignables passent en `Unreachable` ;
* les branches futures atteignables restent `Planned` ;
* la Room passe en `NodeSelected` ;
* `availableNodes` devient vide.

---

## 7.3 Résoudre l’événement courant

Endpoint :

```http
POST /api/v2/runs/{runId}/current-event/resolve
```

Effets :

* le node sélectionné passe en `Resolved` ;
* la Room passe en `NodeResolved` ;
* la Run reste `Active`, sauf si le node résolu est le boss ;
* si le node résolu est le boss :

  * la Room passe en `Completed` ;
  * la Run passe en `RoomResolved`.

---

## 7.4 Progresser dans la Room

Endpoint :

```http
POST /api/v2/runs/{runId}/progress
```

Effets :

* vérifie que la Room est en `NodeResolved` ;
* récupère le node résolu à la profondeur courante ;
* passe à la profondeur suivante ;
* débloque uniquement les enfants directs du node résolu ;
* met ces enfants en `Available` ;
* passe la Room en `Active` ou `BossReached`.

Si la prochaine profondeur contient le boss :

```text
RoomState = BossReached
AvailableNodes = [RoomBossNode]
```

---

# 8. Évolution du générateur

## 8.1 Version

Le générateur est actuellement identifié par :

```text
GeneratorVersion = gen-0.2.0
MarkovMatrixVersion = markov-0.2.0
```

Même si la vraie matrice de Markov n’est pas encore pleinement introduite, la version indique que la génération a changé de structure.

---

## 8.2 RoomPlan complet

Le générateur produit maintenant :

* une room complète ;
* un nombre de nodes entre 6 et 10 ;
* plusieurs couches de nodes ;
* un boss final unique ;
* un graphe convergent ;
* des nodes multi-events ;
* des contraintes d’événements.

---

## 8.3 Génération convergente

Le générateur doit garantir :

```text
Chaque node non-boss possède au moins un enfant.
Chaque node non-boss possède un chemin vers le boss.
Tous les nodes de l’avant-dernière profondeur mènent au boss.
Le boss est seul sur la dernière profondeur.
```

---

## 8.4 Contraintes de génération d’events

Un état interne de génération suit les compteurs d’événements par Room.

Cet état permet de contrôler :

* le nombre de `Rest` ;
* le nombre de `Npc` ;
* le nombre de `Elite` ;
* le nombre de `Item` ;
* le nombre de `Rare` ;
* le nombre de `Merchant` ;
* le nombre de `Law` ;
* le nombre de `Curse`.

---

# 9. Tests ajoutés ou adaptés

## 9.1 Tests unitaires Domain

Les tests Domain couvrent notamment :

* création d’une run active ;
* validation du nombre de nodes ;
* sélection d’un node ;
* verrouillage des siblings ;
* marquage des branches inaccessibles ;
* résolution du node courant ;
* progression vers les enfants ;
* arrivée au boss ;
* résolution du boss ;
* passage de la Room en `Completed` ;
* passage de la Run en `RoomResolved`.

---

## 9.2 Tests unitaires Generator

Les tests du générateur couvrent notamment :

* génération d’une Room de 6 à 10 nodes ;
* génération de nodes avec 1 à 4 événements ;
* absence de `Memory` dans le plan visible ;
* unicité du boss ;
* boss seul à la dernière profondeur ;
* respect des contraintes par type d’événement ;
* respect des contraintes par node ;
* convergence vers le boss ;
* présence d’au moins un enfant pour chaque node non-boss.

---

## 9.3 Tests unitaires Application

Les handlers testés incluent notamment :

* `StartRunCommandHandler` ;
* `ChooseNodeCommandHandler` ;
* `ResolveCurrentEventCommandHandler` ;
* `ProgressRunCommandHandler`.

Les tests vérifient que les handlers appellent bien le repository, modifient correctement l’état de la run et retournent des DTO cohérents.

---

## 9.4 Tests d’intégration API

Les tests d’intégration couvrent :

* démarrage d’une run ;
* validation d’erreur sur playerId vide ;
* choix d’un node ;
* refus d’un second choix à la même profondeur ;
* refus de choisir un node `Planned` ;
* résolution de l’événement courant ;
* progression vers les enfants du node résolu ;
* progression jusqu’au boss ;
* résolution du boss ;
* erreurs `404` sur run inconnue ;
* erreurs `400` sur progression invalide.

---

# 10. Vérifications API manuelles

Des vérifications manuelles via Swagger ont confirmé :

* génération d’une Room à 10 nodes ;
* absence de `Memory` dans `eventTypes` ;
* présence d’un seul `Rest` ;
* `Rest` exclusif dans son node ;
* `Rest` avec `rewardProfile = healing-only` ;
* `Npc` limité à une occurrence ;
* `Item` limité correctement ;
* boss unique ;
* boss seul à la dernière profondeur ;
* progression correcte après résolution d’un node ;
* `/progress` débloque les enfants du node résolu ;
* le chemin atteint bien le boss.

---

# 11. Limites connues

## 11.1 Markov n’est pas encore réellement structurant

La génération est encore principalement déterministe par seed et contraintes.

La vraie logique Markov doit être introduite dans un jalon suivant.

La cible est :

```text
Markov propose.
Les contraintes métier filtrent.
La seed rend le résultat reproductible.
```

Markov devra s’appliquer à deux niveaux :

```text
RoomType → RoomType
NodeEventType → NodeEventType
```

---

## 11.2 Les formes de graphe sont parfois très convergentes

Certaines Rooms peuvent produire des formes du type :

```text
4 → 1 → 3 → 1 → Boss
```

C’est valide, mais pas toujours optimal en termes de sensation de choix.

À terme, le générateur devra favoriser des structures plus variées :

```text
3 → 2 → 2 → Boss
4 → 3 → 2 → Boss
2 → 3 → 2 → Boss
```

---

## 11.3 Le champ `ParentNodeId` est temporaire

`ParentNodeId` reste exposé pour compatibilité.

Le champ cible est :

```text
ParentNodeIds
```

Le frontend devra utiliser `parentNodeIds`.

---

## 11.4 La résolution réelle des événements n’existe pas encore

Pour l’instant, `current-event/resolve` résout le node dans son ensemble.

À terme, il faudra résoudre réellement :

* chaque événement du node ;
* les combats ;
* les soins ;
* les marchands ;
* les lois ;
* les malédictions ;
* les événements rares ;
* les objets ;
* les apparitions dynamiques de `Memory`.

---

## 11.5 La persistance est encore in-memory

Le repository actuel est temporaire.

La persistance finale devra s’appuyer sur :

* PostgreSQL ;
* Event Store ;
* éventuellement snapshots ;
* audit trail ;
* événements de domaine persistés.

---

# 12. Prochaines étapes recommandées

## 12.1 Générer la room suivante

Prochaine étape fonctionnelle recommandée :

```http
POST /api/v2/runs/{runId}/rooms/next
```

Préconditions :

```text
RunStatus = RoomResolved
CurrentRoom.State = Completed
```

Effets :

* générer une nouvelle Room ;
* choisir son `RoomType` ;
* déterminer son boss ;
* générer son RoomPlan complet ;
* respecter les mêmes contraintes ;
* remettre la Run en `Active`.

---

## 12.2 Introduire la vraie matrice de Markov

Créer des composants dédiés :

```text
MarkovRoomTransitionMatrix
MarkovNodeEventTransitionMatrix
WeightedRandomPicker
RoomGenerationContext
NodeGenerationContext
```

Markov devra influencer :

* le type de la room suivante ;
* les événements visibles proposés ;
* la difficulté ;
* les risques ;
* les patterns de room ;
* les probabilités d’événements rares ;
* les futures apparitions de `Memory`.

---

## 12.3 Introduire la résolution réelle des NodeEvents

Actuellement, le node entier est résolu en bloc.

À terme, il faudra introduire :

```text
NodeEventResolution
CombatResolution
RestResolution
RewardResolution
MemoryProcResolution
LawResolution
CurseResolution
MerchantResolution
```

---

## 12.4 Préparer l’intégration frontend

Le frontend devra consommer :

```text
nodeLayers
parentNodeIds
availableNodes
state
eventTypes
bossPreview
```

Cela permettra d’afficher :

* la carte complète de la Room ;
* les branches accessibles ;
* les branches verrouillées ;
* les branches inaccessibles ;
* la position courante du joueur ;
* le boss à venir.

---

# 13. Conclusion

Ce jalon stabilise une partie centrale du gameplay de **L’épopée des silences**.

La Room est désormais :

* visible dès son entrée ;
* composée de 6 à 10 nodes ;
* organisée en couches ;
* structurée en chemins ;
* convergente vers un boss ;
* contrôlée par des contraintes d’événements ;
* compatible avec une future matrice Markov ;
* exposée proprement via API ;
* protégée par tests unitaires et tests d’intégration.

Cette base est suffisamment solide pour poursuivre vers :

* génération de la room suivante ;
* vraie matrice de Markov ;
* résolution détaillée des événements ;
* persistance événementielle ;
* affichage frontend de la carte.
