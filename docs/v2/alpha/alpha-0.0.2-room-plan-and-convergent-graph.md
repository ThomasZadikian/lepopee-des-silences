# L’épopée des silences — SPEC-TECH alpha.3

## Room Plan Generation & Internal Progression Foundation

## 1. Objet du document

Ce document complète le précédent suivi technique `alpha.2 — Game Engine Foundation`.

Il formalise les évolutions réalisées depuis la mise en place de la première verticale API `StartRun / GetRunById / ChooseNode / ResolveCurrentEvent`.

Ce jalon stabilise une partie centrale du gameplay de **L’épopée des silences** : la génération complète d’une Room, sa représentation sous forme de plan visible, sa progression interne, ses contraintes de nodes, ses contraintes d’événements, et la séparation propre des responsabilités de génération.

Ce document a pour objectifs :

* documenter les décisions prises ;
* expliquer les refactors effectués ;
* clarifier les règles métier stabilisées ;
* garder une trace des choix d’architecture ;
* préparer proprement la suite : génération de la room suivante, persistance événementielle, vraie matrice Markov et frontend de carte.

---

## 2. Contexte du jalon

À la fin du jalon précédent, la boucle serveur minimale était :

```text
POST /api/v2/runs
GET  /api/v2/runs/{runId}
POST /api/v2/runs/{runId}/nodes/{nodeId}/choose
POST /api/v2/runs/{runId}/current-event/resolve
```

Cette boucle permettait déjà de :

* créer une run ;
* récupérer son état courant ;
* sélectionner un node ;
* résoudre l’événement courant.

Cependant, le modèle initial avait encore une ambiguïté importante : une Room était traitée trop simplement comme une étape courte, alors que la vision gameplay finale exige une structure plus riche.

La clarification métier a été la suivante :

```text
Une Room n’est pas un node.
Une Room est un ensemble organisé de nodes.
Une Room contient plusieurs couches de nodes.
Le joueur progresse de couche en couche.
Toutes les branches convergent vers un boss de room.
La victoire contre ce boss débloque la room suivante.
```

Cette clarification a entraîné un refactor important du domaine et du générateur.

---

## 3. Règles métier stabilisées

Les règles suivantes sont désormais considérées comme structurantes pour le gameplay v2.

### 3.1 Room

Une Room représente une zone complète du Palais mental.

Elle contient :

* un type de room ;
* un thème ;
* un boss de room ;
* un plan complet visible ;
* entre 6 et 10 nodes ;
* des nodes organisés en couches ;
* une progression interne ;
* un état de progression ;
* des nodes disponibles à la profondeur actuelle ;
* des nodes futurs visibles mais non sélectionnables.

Règles principales :

```text
Une Room contient entre 6 et 10 nodes.
Une Room contient exactement un boss de room.
Le boss de room est placé au dernier niveau de profondeur interne.
Les couches de nodes sont continues.
Les nodes de profondeur 0 sont Available au lancement.
Les nodes futurs sont Planned.
Toutes les branches doivent converger vers le boss.
La room n’est Completed qu’après résolution du boss.
```

### 3.2 Node

Un Node représente un choix ou une étape dans une Room.

Un Node contient désormais :

* un identifiant ;
* une profondeur interne ;
* un ou plusieurs parents ;
* un état ;
* un niveau de risque ;
* un profil de récompense ;
* une collection d’événements ;
* une indication éventuelle de boss de room.

Règles principales :

```text
Un Node contient entre 1 et 4 events.
Un Node peut être Available, Planned, Selected, Locked, Resolved ou Unreachable.
Un Node Available peut être choisi.
Un Node Planned est visible mais non sélectionnable.
Un Node Selected représente le choix courant.
Un Node Resolved représente un choix terminé.
Un RoomBossNode contient l’événement RoomBoss.
```

### 3.3 NodeEvent

Un Node ne porte plus un seul type d’événement simple. Il porte une collection de `NodeEvent`.

Chaque `NodeEvent` contient :

* un type d’événement ;
* un ordre dans le node.

Cela permet de représenter des nodes composites :

```text
Node A : Combat
Node B : Memory + Rest
Node C : Law + Combat + Item
Node D : Merchant + Curse
```

La structure prépare les futures résolutions détaillées :

* combat exact ;
* récompense exacte ;
* texte narratif ;
* effet de loi ;
* effet de malédiction ;
* mémoire débloquée ;
* conséquence cachée éventuelle.

---

## 4. Décision de visibilité joueur

Une décision importante a été prise à partir de retours joueurs.

Le joueur doit connaître au lancement d’une room :

* le nombre de nodes avant le boss ;
* le nombre d’événements par node ;
* la nature générale des événements de chaque node ;
* la structure des chemins ;
* le boss de room annoncé.

En revanche, le joueur ne connaît pas :

* l’ennemi exact ;
* les statistiques exactes ;
* les récompenses exactes ;
* les textes narratifs exacts ;
* les effets numériques exacts ;
* certaines conséquences cachées ;
* les tirages internes.

La règle retenue est donc :

```text
Le plan stratégique est visible.
La résolution exacte reste serveur-autoritaire.
```

Cette décision donne au joueur de vrais choix stratégiques sans supprimer l’incertitude du roguelite.

---

## 5. Modèle de progression interne d’une Room

La Room fonctionne maintenant comme une progression en couches.

Exemple conceptuel :

```text
Room: Memory
Boss: Archiviste Fêlé

Depth 0
├── Node A: Combat
├── Node B: Npc + Law
└── Node C: Rest

Depth 1
├── Node D: Combat + Item
└── Node E: Merchant

Depth 2
├── Node F: Elite
└── Node G: Curse + Rare

Depth 3
└── Node H: RoomBoss
```

Au lancement :

```text
Depth 0 = Available
Depth 1+ = Planned
Boss = Planned
```

Pendant la progression :

```text
ChooseNode
→ ResolveCurrentEvent
→ ProgressCurrentRoom
→ prochaine couche Available
```

À la fin :

```text
Choose RoomBossNode
→ ResolveCurrentEvent
→ Room Completed
→ Run RoomResolved
```

---

## 6. États de Room

La Room possède désormais un état de progression.

Les états principaux sont :

```text
Active       : le joueur peut choisir un node disponible.
NodeSelected : un node a été choisi et attend résolution.
NodeResolved : l’événement du node courant est résolu.
BossReached  : le boss de room est accessible.
Completed    : le boss de room est résolu.
```

La Run ne peut passer à la Room suivante que si :

```text
CurrentRoom.State == Completed
Run.Status == RoomResolved
```

---

## 7. États de Node

Les états de Node permettent de représenter le plan complet de la Room sans rendre tous les choix immédiatement accessibles.

États utilisés :

```text
Planned     : visible sur le plan, mais pas encore sélectionnable.
Available   : sélectionnable maintenant.
Selected    : choisi, événement en cours.
Locked      : non choisi à la profondeur courante.
Resolved    : choisi et terminé.
Unreachable : branche rendue inaccessible par les choix précédents.
```

Cette distinction est essentielle pour le futur affichage de carte.

---

## 8. Contraintes d’événements

Les contraintes suivantes ont été stabilisées :

```text
Combat : illimité.
Elite : maximum 1 par room.
Item : maximum 1 par node et maximum totalNodeCount / 2 par room.
Npc : maximum 1 par room.
Memory : non planifié comme node event standard.
Rest : maximum 1 par room.
Rest : doit être seul dans son node.
Merchant : maximum 1 par room.
Law : maximum 1 par room.
Curse : maximum 1 par room.
Rare : maximum 3 par room.
RoomBoss : exactement 1, dans le boss node.
```

Le profil de récompense `Rest` a été ajusté :

```text
Rest seul → rewardProfile = healing-only
```

Cela clarifie le fait que Rest sert au soin et ne constitue pas une récompense classique.

---

## 9. Boss dépendant du type de Room

Le boss de room dépend directement du `RoomType`.

Exemples :

```text
Threshold   → Gardien du Seuil
Memory      → Archiviste Fêlé
Forest      → Cerf de Cendre
Rupture     → Fragment Brisé
Silence     → Veilleur Muet
Antechamber → Gardien de l’Antichambre
Final       → Him’Lit
```

Le boss est exposé dans le DTO de room sous forme de preview.

Cette décision est importante pour la planification joueur : le joueur sait vers quel affrontement la room converge.

---

## 10. API et DTOs

La réponse API expose maintenant la Room comme un plan.

La Room DTO contient notamment :

* `roomType` ;
* `theme` ;
* `state` ;
* `currentNodeDepth` ;
* `maxNodeDepth` ;
* `totalNodeCount` ;
* `bossPreview` ;
* `nodeLayers` ;
* `availableNodes`.

Les nodes sont regroupés en couches :

```json
{
  "nodeLayers": [
    {
      "depth": 0,
      "nodes": []
    },
    {
      "depth": 1,
      "nodes": []
    }
  ]
}
```

Cela évite d’exposer une simple liste plate illisible et prépare l’affichage frontend de carte.

---

## 11. Endpoint de progression

L’ancien raisonnement autour de `/nodes/next` a été abandonné.

Dans le nouveau modèle, les nodes ne sont plus générés progressivement après chaque résolution. La Room complète est générée dès son entrée.

La progression consiste donc à déverrouiller la couche suivante, pas à générer de nouveaux nodes.

L’endpoint utilisé est :

```http
POST /api/v2/runs/{runId}/progress
```

Il permet de faire avancer la Room après résolution du node courant.

Flux actuel :

```text
POST /api/v2/runs
→ Room complète générée

POST /api/v2/runs/{runId}/nodes/{nodeId}/choose
→ Node choisi

POST /api/v2/runs/{runId}/current-event/resolve
→ Node résolu

POST /api/v2/runs/{runId}/progress
→ Couche suivante disponible
```

---

## 12. Refactor du générateur déterministe

Le fichier `DeterministicRunGenerator` était devenu trop volumineux et contenait trop de responsabilités.

Il mélangeait :

* seed ;
* random déterministe ;
* choix du type de room ;
* choix du boss ;
* génération du plan ;
* répartition en couches ;
* création de nodes ;
* génération d’événements ;
* contraintes d’événements ;
* calcul du risque ;
* calcul du reward profile ;
* thème de room.

Cette structure risquait de devenir une `God class`.

Un refactor a donc été décidé pour appliquer les principes SOLID dès maintenant.

---

## 13. Nouvelle architecture de génération

La génération a été découpée en composants spécialisés.

Structure cible :

```text
Infrastructure/Generation/
├── DeterministicRunGenerator.cs
├── Common/
│   └── RoomGenerationConstants.cs
├── Randomness/
│   ├── ISeededRandomFactory.cs
│   └── SeededRandomFactory.cs
└── Rooms/
    ├── Planning/
    │   ├── IRoomPlanGenerator.cs
    │   └── RoomPlanGenerator.cs
    ├── Types/
    │   ├── IRoomTypeResolver.cs
    │   └── RoomTypeResolver.cs
    ├── Themes/
    │   ├── IRoomThemeResolver.cs
    │   └── RoomThemeResolver.cs
    ├── Bosses/
    │   ├── IRoomBossProfileResolver.cs
    │   └── RoomBossProfileResolver.cs
    ├── Events/
    │   ├── IRoomEventGenerationState.cs
    │   ├── RoomEventGenerationState.cs
    │   ├── IRoomEventGenerationStateFactory.cs
    │   ├── RoomEventGenerationStateFactory.cs
    │   ├── INodeEventCandidateResolver.cs
    │   ├── NodeEventCandidateResolver.cs
    │   ├── INodeEventGenerator.cs
    │   └── NodeEventGenerator.cs
    ├── Layers/
    │   ├── IRoomNodeLayerPlanner.cs
    │   └── RoomNodeLayerPlanner.cs
    ├── Nodes/
    │   ├── IRoomNodeFactory.cs
    │   └── RoomNodeFactory.cs
    ├── Risk/
    │   ├── INodeRiskResolver.cs
    │   └── NodeRiskResolver.cs
    └── Rewards/
        ├── INodeRewardProfileResolver.cs
        └── NodeRewardProfileResolver.cs
```

---

## 14. Responsabilités des composants

### DeterministicRunGenerator

Responsabilité :

```text
Orchestrer la génération d’une room initiale ou suivante.
```

Il dépend de :

* `ISeededRandomFactory` ;
* `IRoomTypeResolver` ;
* `IRoomPlanGenerator`.

Il ne contient plus la logique détaillée de construction du plan.

### SeededRandomFactory

Responsabilité :

```text
Créer un Random déterministe à partir de seed + roomDepth + generatorVersion.
```

Cela centralise la reproductibilité.

### RoomTypeResolver

Responsabilité :

```text
Choisir le type de room selon la profondeur et le hasard déterministe.
```

Règles :

```text
depth 0     → Threshold
depth >= 10 → Final
sinon       → Memory / Forest / Rupture / Silence / Antechamber
```

### RoomThemeResolver

Responsabilité :

```text
Convertir un RoomType en thème textuel.
```

### RoomBossProfileResolver

Responsabilité :

```text
Associer un boss au type de room.
```

### RoomPlanGenerator

Responsabilité :

```text
Assembler une Room complète valide.
```

Il orchestre :

* total node count ;
* génération des couches ;
* création du boss ;
* création finale de `Room`.

### RoomNodeLayerPlanner

Responsabilité :

```text
Déterminer combien de couches normales existent et combien de nodes placer dans chaque couche.
```

### RoomNodeFactory

Responsabilité :

```text
Créer les nodes normaux et le boss node.
```

Il ne décide pas directement des events, risques ou rewards. Il délègue.

### NodeEventGenerator

Responsabilité :

```text
Générer entre 1 et 4 événements valides pour un node.
```

### NodeEventCandidateResolver

Responsabilité :

```text
Construire la liste des événements candidats selon les contraintes de room.
```

C’est ici que vivent les règles comme :

* Elite max 1 ;
* Rest singleton ;
* Memory non planifié ;
* Item max par node ;
* etc.

### RoomEventGenerationState

Responsabilité :

```text
Suivre les événements déjà générés dans la room.
```

### NodeRiskResolver

Responsabilité :

```text
Calculer le risque global d’un node à partir de ses events.
```

### NodeRewardProfileResolver

Responsabilité :

```text
Déterminer le rewardProfile global du node.
```

---

## 15. Application stricte de Clean Architecture

La couche `Application` ne connaît pas les détails de génération.

Elle dépend uniquement de :

```text
IRunGenerator
```

Les composants suivants restent dans `Infrastructure`, car ils sont des détails internes de l’implémentation déterministe :

```text
ISeededRandomFactory
IRoomTypeResolver
IRoomPlanGenerator
IRoomBossProfileResolver
IRoomThemeResolver
INodeEventGenerator
INodeEventCandidateResolver
INodeRiskResolver
INodeRewardProfileResolver
IRoomNodeFactory
IRoomNodeLayerPlanner
```

Cela respecte la frontière :

```text
Application dit : je veux une Room.
Infrastructure décide : comment cette Room est générée.
```

---

## 16. Principes SOLID appliqués

### SRP — Single Responsibility Principle

Chaque composant possède une responsabilité claire :

```text
RoomTypeResolver       → type de room
RoomBossProfileResolver → boss
RoomThemeResolver      → thème
NodeEventGenerator     → events
NodeRiskResolver       → risque
NodeRewardProfileResolver → reward profile
RoomNodeLayerPlanner   → répartition en couches
RoomNodeFactory        → création de nodes
RoomPlanGenerator      → assemblage de room
```

### OCP — Open/Closed Principle

On pourra ajouter :

* un autre générateur ;
* une vraie matrice Markov ;
* un générateur saisonnier ;
* un générateur de difficulté ;
* un générateur narratif ;

sans casser l’Application.

### DIP — Dependency Inversion Principle

`Application` dépend de `IRunGenerator`.

`Infrastructure` fournit `DeterministicRunGenerator`.

### ISP — Interface Segregation Principle

Les interfaces sont petites et spécialisées.

Aucune interface massive de type `IGenerationService` n’a été créée.

---

## 17. Tests et couverture

Les tests existants ont été adaptés pour vérifier le nouveau modèle :

* génération d’une room complète ;
* total nodes entre 6 et 10 ;
* 1 à 4 events par node ;
* boss unique ;
* boss au dernier depth ;
* layers continues ;
* nodes initiaux disponibles ;
* nodes futurs planned ;
* branches convergentes ;
* contraintes d’événements ;
* absence de Memory planifié ;
* Rest singleton ;
* Rest avec reward profile `healing-only`.

Les tests de génération traversent désormais une chaîne plus propre :

```text
DeterministicRunGenerator
→ SeededRandomFactory
→ RoomTypeResolver
→ RoomPlanGenerator
→ RoomNodeLayerPlanner
→ RoomNodeFactory
→ NodeEventGenerator
→ NodeEventCandidateResolver
→ NodeRiskResolver
→ NodeRewardProfileResolver
→ RoomBossProfileResolver
→ RoomThemeResolver
→ Domain Room / Node / NodeEvent
```

Cela donne un coverage fonctionnel utile sur la génération.

Des tests plus ciblés pourront être ajoutés ensuite pour renforcer :

* `RoomTypeResolverTests` ;
* `RoomBossProfileResolverTests` ;
* `NodeEventCandidateResolverTests` ;
* `NodeRewardProfileResolverTests` ;
* `NodeRiskResolverTests` ;
* `RoomNodeLayerPlannerTests` ;
* `SeededRandomFactoryTests`.

---

## 18. État actuel validé

À ce jalon, la génération de Room est désormais :

```text
- structurée ;
- testée ;
- déterministe ;
- compatible avec seed ;
- compatible avec version de générateur ;
- compatible avec version de matrice Markov ;
- organisée par responsabilités ;
- prête pour la génération de room suivante ;
- prête pour la future persistance événementielle ;
- prête pour le futur affichage frontend de carte.
```

---

## 19. Prochaines étapes recommandées

### 19.1 Commit du refactor

Commit recommandé :

```bash
refactor(game-engine): split room generation architecture
```

### 19.2 Ajouter des tests unitaires ciblés

Après ce commit, ajouter un petit jalon qualité :

```text
test(game-engine): cover room generation resolvers
```

Tests recommandés :

* `SeededRandomFactoryTests`
* `RoomTypeResolverTests`
* `RoomBossProfileResolverTests`
* `RoomThemeResolverTests`
* `NodeEventCandidateResolverTests`
* `NodeRewardProfileResolverTests`
* `NodeRiskResolverTests`
* `RoomNodeLayerPlannerTests`

### 19.3 Implémenter MoveToNextRoom

Prochaine feature métier :

```http
POST /api/v2/runs/{runId}/rooms/next
```

Règles :

```text
Seulement si CurrentRoom.State == Completed.
Seulement si Run.Status == RoomResolved.
Génère une nouvelle Room complète.
Ajoute la Room à la Run.
Passe la Run en Active.
Si room finale, passe vers BossReached / Final.
```

### 19.4 Préparer l’Event Store

Une fois la boucle complète stabilisée :

```text
StartRun
→ ChooseNode
→ ResolveCurrentEvent
→ ProgressCurrentRoom
→ CompleteRoom
→ MoveToNextRoom
```

on pourra introduire l’Event Sourcing :

```text
RunStarted
RoomGenerated
NodeChosen
NodeResolved
RoomProgressed
RoomCompleted
NextRoomGenerated
```

---

## 20. Conclusion

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
* protégée par tests unitaires et tests d’intégration ;
* générée par une architecture propre, découpée et maintenable.

Cette base est suffisamment solide pour poursuivre vers :

* génération de la room suivante ;
* vraie matrice de Markov ;
* résolution détaillée des événements ;
* persistance événementielle ;
* affichage frontend de la carte.
