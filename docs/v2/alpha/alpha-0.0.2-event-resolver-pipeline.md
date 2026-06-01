# L’épopée des silences — Suivi technique alpha0.0.2

## Session — Pipeline de résolution des événements de nœud

**Projet :** L’épopée des silences  
**Ancien nom :** RPG_ESI07  
**Branche cible :** `v2/develop`  
**Service concerné :** `services/game-engine`  
**Version de travail :** `alpha0.0.2`  
**Commit recommandé :** `feat(game-engine): add node event resolver pipeline`

---

## 1. Contexte de la session

La session s’inscrit dans la refonte v2 du projet, dont l’objectif est de transformer RPG_ESI07 en **RPG roguelite narratif full web**, centré sur l’exploration du Palais mental du joueur.

Le socle déjà en place avant cette session permettait de gérer la boucle principale :

```text
StartRun
GetRunById
ChooseNode
ResolveCurrentEvent
ProgressRun
MoveToNextRoom
AbandonRun
```

Le système savait donc :

- créer une run ;
- générer une première room ;
- afficher des nodes disponibles ;
- sélectionner un node ;
- résoudre techniquement le node ;
- progresser vers les enfants accessibles ;
- converger vers le boss de room ;
- changer de room ;
- abandonner une run.

Cependant, `ResolveCurrentEvent` restait encore trop pauvre fonctionnellement : il marquait un node comme résolu, mais ne produisait pas encore de résultat métier exploitable par le frontend.

Or, dans la vision finale, un node ne doit pas être une simple étape technique. Il doit représenter un événement réel du Palais : combat, repos, objet, PNJ, Loi du Palais, marchand, malédiction, événement rare, boss de room, etc.

La session a donc eu pour objectif de préparer la couche suivante : **la signification métier et narrative des nodes**.

---

## 2. Problème identifié

L’architecture précédente permettait de résoudre un node, mais elle ne répondait pas encore à une question centrale :

> Que se passe-t-il réellement quand le joueur résout ce node ?

Sans pipeline dédiée, le risque aurait été de faire évoluer `ResolveCurrentEventCommandHandler` vers un gros bloc conditionnel contenant toute la logique :

```csharp
if eventType == Combat ...
if eventType == Law ...
if eventType == Npc ...
if eventType == Merchant ...
```

Cette approche aurait rapidement créé :

- un handler trop volumineux ;
- un couplage fort entre run, combat, récompenses, narration et lois ;
- une difficulté à ajouter de nouveaux types d’événements ;
- une dette technique importante ;
- une architecture contraire à la vision microservices / modules spécialisés de la v2.

La décision prise est donc de créer une **pipeline de résolution d’événements** extensible.

---

## 3. Décision d’architecture

La résolution d’un événement ne doit pas être portée directement par `ResolveCurrentEventCommandHandler`.

Le handler doit rester un orchestrateur applicatif :

```text
ResolveCurrentEventCommandHandler
→ récupérer la run
→ identifier le node sélectionné
→ construire un contexte de résolution
→ déléguer au dispatcher
→ résoudre techniquement le node
→ persister la run
→ retourner RunDto + Outcome
```

La logique spécifique à chaque type d’événement est déléguée à des resolvers spécialisés :

```text
INodeEventResolver
├── CombatNodeEventResolver
├── EliteNodeEventResolver
├── ItemNodeEventResolver
├── NpcNodeEventResolver
├── RestNodeEventResolver
├── MerchantNodeEventResolver
├── LawNodeEventResolver
├── CurseNodeEventResolver
├── RareNodeEventResolver
├── RoomBossNodeEventResolver
└── FinalBossNodeEventResolver
```

Le dispatcher choisit le resolver adapté selon le `NodeEventType` principal du node.

Cette architecture est volontairement plus structurée que nécessaire pour une alpha, parce qu’elle est alignée avec l’architecture finale attendue.

---

## 4. Éléments ajoutés

### 4.1 DTOs d’outcome événementiel

Création d’un dossier applicatif :

```text
src/Leds.GameEngine.Application/Events/Dtos
```

DTOs ajoutés :

```text
NarrativeFragmentDto
NodeEventChoiceDto
ResolvedNodeEventOutcomeDto
```

Ces objets permettent à l’API de retourner autre chose qu’un simple état de run.

Le frontend pourra désormais recevoir un résultat exploitable :

```json
{
  "run": {},
  "outcome": {
    "nodeId": "...",
    "eventTypes": ["Combat"],
    "primaryEventType": "Combat",
    "resolutionKind": "CombatStarted",
    "riskLevel": 42,
    "rewardProfile": "combat-common",
    "title": "Résonance hostile",
    "description": "Une présence hostile se manifeste dans le Palais.",
    "requiresPlayerChoice": false,
    "choices": [],
    "narrativeFragments": []
  }
}
```

---

### 4.2 Contrats de résolution d’événements

Création du dossier :

```text
src/Leds.GameEngine.Application/Events/ResolveNodeEvent
```

Éléments ajoutés :

```text
NodeEventResolutionKind
NodeEventResolutionContext
NodeEventResolutionResult
INodeEventResolver
INodeEventResolverDispatcher
NodeEventResolverDispatcher
```

Responsabilités :

- `NodeEventResolutionContext` transporte la run, la room et le node à résoudre.
- `NodeEventResolutionResult` représente le résultat métier brut retourné par un resolver.
- `INodeEventResolver` définit le contrat de résolution spécialisé.
- `NodeEventResolverDispatcher` sélectionne le bon resolver selon le type principal du node.

---

### 4.3 Resolvers MVP

Création du dossier :

```text
src/Leds.GameEngine.Application/Events/Resolvers
```

Resolvers ajoutés :

```text
CombatNodeEventResolver
EliteNodeEventResolver
ItemNodeEventResolver
NpcNodeEventResolver
RestNodeEventResolver
MerchantNodeEventResolver
LawNodeEventResolver
CurseNodeEventResolver
RareNodeEventResolver
RoomBossNodeEventResolver
FinalBossNodeEventResolver
```

À ce stade, les resolvers produisent des outcomes simples, sans encore appliquer de conséquences profondes.

Ils constituent néanmoins la bonne charpente pour brancher plus tard :

- le moteur de combat ;
- le moteur de récompenses ;
- le moteur des Lois du Palais ;
- le système narratif ;
- Elise ;
- le Tome Writer ;
- les projections Event Sourcing.

---

## 5. Modification de `ResolveCurrentEvent`

### 5.1 Avant

`ResolveCurrentEvent` retournait uniquement :

```csharp
public sealed record ResolveCurrentEventResponse(RunDto Run);
```

Le handler appelait principalement :

```csharp
run.ResolveCurrentEvent();
```

Puis persistait la run.

---

### 5.2 Après

La réponse retourne désormais :

```csharp
public sealed record ResolveCurrentEventResponse(
    RunDto Run,
    ResolvedNodeEventOutcomeDto Outcome);
```

Le handler :

1. récupère la run ;
2. identifie le node sélectionné à la profondeur courante ;
3. construit un `NodeEventResolutionContext` ;
4. appelle `INodeEventResolverDispatcher` ;
5. résout techniquement le node ;
6. persiste la run ;
7. retourne `RunDto` + `Outcome`.

Cette évolution marque le passage de :

```text
résolution technique d’un node
```

à :

```text
résolution métier d’un événement du Palais
```

---

## 6. Injection de dépendances

Les resolvers et le dispatcher ont été enregistrés dans la couche Application.

Services ajoutés :

```text
INodeEventResolverDispatcher → NodeEventResolverDispatcher
INodeEventResolver → CombatNodeEventResolver
INodeEventResolver → EliteNodeEventResolver
INodeEventResolver → ItemNodeEventResolver
INodeEventResolver → NpcNodeEventResolver
INodeEventResolver → RestNodeEventResolver
INodeEventResolver → MerchantNodeEventResolver
INodeEventResolver → LawNodeEventResolver
INodeEventResolver → CurseNodeEventResolver
INodeEventResolver → RareNodeEventResolver
INodeEventResolver → RoomBossNodeEventResolver
INodeEventResolver → FinalBossNodeEventResolver
```

L’enregistrement explicite est volontaire pour l’alpha : il rend les dépendances visibles et évite une découverte automatique trop magique à ce stade.

---

## 7. Tests ajoutés ou adaptés

### 7.1 Tests unitaires du dispatcher

Ajout du test :

```text
tests/Leds.GameEngine.UnitTests/Events/ResolveNodeEvent/NodeEventResolverDispatcherTests.cs
```

Objectifs :

- vérifier que le dispatcher sélectionne le resolver correspondant au `NodeEventType` principal ;
- vérifier qu’une `DomainException` est levée si aucun resolver n’est enregistré pour le type demandé.

---

### 7.2 Factory de contexte de résolution

Ajout / correction de :

```text
tests/Leds.GameEngine.UnitTests/Common/Factories/TestNodeEventResolutionContextFactory.cs
```

Correction effectuée pendant la session :

- l’ancienne version utilisait `room.AvailableNodes.Single()` ;
- cela échouait lorsque plusieurs nodes étaient disponibles ;
- la nouvelle version conserve explicitement la référence du node cible.

Cette correction rend le test stable et évite de lier le test du dispatcher à une hypothèse incorrecte sur le nombre de nodes disponibles.

---

### 7.3 Tests unitaires du handler `ResolveCurrentEvent`

Les tests existants ont été adaptés au nouveau constructeur :

```csharp
ResolveCurrentEventCommandHandler(
    IRunRepository runRepository,
    INodeEventResolverDispatcher nodeEventResolverDispatcher)
```

Un mock de `INodeEventResolverDispatcher` est maintenant injecté dans les tests unitaires.

Les assertions ont également été ajustées pour vérifier l’existence de l’outcome.

---

### 7.4 Tests d’intégration de l’endpoint

Ajout ou adaptation d’un test vérifiant que :

```text
POST /api/v2/runs/{runId}/current-event/resolve
```

retourne désormais :

- la run ;
- un outcome ;
- le node résolu ;
- les event types ;
- un `ResolutionKind` ;
- un titre ;
- une description ;
- un niveau de risque ;
- un reward profile ;
- une collection de fragments narratifs.

---

## 8. Problèmes rencontrés et corrections

### 8.1 Constructeur du handler modifié

Erreur rencontrée :

```text
Aucun argument ne correspond au paramètre obligatoire 'nodeEventResolverDispatcher'
```

Cause : les tests unitaires instancient directement `ResolveCurrentEventCommandHandler`, dont le constructeur a changé.

Correction : injection d’un mock de `INodeEventResolverDispatcher` dans les tests.

---

### 8.2 Message d’erreur métier changé

Ancien message attendu :

```text
Room must have a selected node before resolving an event.
```

Nouveau message réel :

```text
No node has been selected for the current room depth.
```

Décision : conserver le nouveau message, plus précis et plus aligné avec la progression par profondeur de room.

Tests corrigés :

- test unitaire `ResolveCurrentEventCommandHandlerTests` ;
- test d’intégration `ResolveCurrentEventEndpointTests`.

---

### 8.3 Factory de test trop restrictive

Erreur rencontrée :

```text
Sequence contains more than one element
```

Cause : utilisation de `Single()` sur les nodes disponibles.

Correction : retour explicite d’un couple `Room + TargetNode` dans le factory.

---

### 8.4 Doublon de factory

Erreur rencontrée :

```text
L'espace de noms contient déjà une définition pour 'TestNodeEventResolutionContextFactory'
```

Cause : deux fichiers déclaraient la même classe dans le même namespace.

Correction : conservation du factory commun dans :

```text
tests/Leds.GameEngine.UnitTests/Common/Factories/TestNodeEventResolutionContextFactory.cs
```

Suppression du doublon.

---

## 9. Pourquoi cette architecture est importante

Cette étape évite de transformer le Game Engine en monolithe interne difficile à maintenir.

Elle prépare une architecture où chaque type d’événement pourra évoluer indépendamment :

```text
Combat → Combat module
Law → Palace Law Engine
Npc → Narrative module
Merchant → Trade/Reward module
Rest → Recovery module
RoomBoss → Boss encounter module
FinalBoss → Him’Lit module
```

Le handler reste stable, même si de nouveaux événements sont ajoutés.

Ajouter un événement futur devra suivre ce modèle :

```text
1. Ajouter le NodeEventType si nécessaire
2. Ajouter le resolver dédié
3. Enregistrer le resolver dans la DI
4. Ajouter la génération éventuelle
5. Ajouter les tests ciblés
```

À plus long terme, l’architecture devra évoluer vers une séparation entre :

```text
NodeEventType      → catégorie technique stable
EventTemplate      → contenu configurable
Runtime Resolution → résolution contextualisée par run
```

Cela permettra d’ajouter beaucoup de contenu sans créer une classe C# pour chaque variation narrative ou gameplay.

---

## 10. Place du scénario dans cette architecture

La session a confirmé un point fondamental : le scénario ne doit pas être traité comme un simple texte affiché côté frontend.

Le scénario doit devenir un sous-système du Game Engine.

À terme, la résolution d’un événement devra tenir compte de :

- la seed ;
- le type de room ;
- le thème de room ;
- le type du node ;
- le niveau de risque ;
- le profil de récompense ;
- les Lois du Palais actives ;
- les choix précédents ;
- les événements déjà résolus ;
- les compagnons ;
- les boss rencontrés ;
- les fragments narratifs déjà révélés ;
- la progression permanente du joueur.

La pipeline ajoutée prépare donc l’intégration future de :

```text
INarrativeFragmentResolver
EliseDialogueResolver
TomeWriter
ScenarioContextBuilder
```

Le scénario reste central dans la vision du projet. Cette session a posé le premier point d’ancrage technique pour l’intégrer proprement.

---

## 11. Vision microservices et statut du backend legacy

La session a également clarifié un enjeu d’architecture globale : le dossier historique `backend/` ne doit plus être considéré comme le backend principal de la v2.

La cible v2 est :

```text
services/game-engine   → cœur gameplay v2
services/identity      → identité/auth future
services/catalog       → référentiels gameplay futurs
services/player        → progression permanente future
services/leaderboard   → classement futur
services/audit-gdpr    → audit/RGPD futur
services/api-gateway   → point d’entrée futur
```

Le dossier `backend/` doit être considéré comme :

```text
legacy v1 / référence historique / preuve RNCP
```

Il ne doit plus recevoir de nouvelles fonctionnalités v2.

Décision recommandée : ajouter une documentation explicite dans :

```text
backend/README.md
services/README.md
README.md
```

afin de rendre claire la frontière entre :

```text
backend/   → legacy v1
services/  → architecture cible v2
```

---

## 12. Versioning

La version de travail reste :

```text
alpha0.0.2
```

Cette décision est volontaire.

La session ajoute une brique d’architecture importante, mais ne constitue pas encore un jalon gameplay complet.

Proposition de découpage :

```text
alpha0.0.2
→ boucle Run/Room/Node
→ génération et progression
→ abandon
→ résolution d’événement avec outcome minimal
→ pipeline de resolvers

alpha0.0.3
→ choix d’événement
→ premiers effets réels de Loi/Récompense/Narration
→ début de scénarisation contextualisée
```

Un bump de version pourra être envisagé après stabilisation de :

```text
POST /api/v2/runs/{runId}/events/{eventId}/choice
```

ou après introduction des premiers effets réels.

---

## 13. État attendu après cette session

Après application des changements, le Game Engine doit permettre :

```text
StartRun
ChooseNode
ResolveCurrentEvent → Run + Outcome
ProgressRun
MoveToNextRoom
AbandonRun
```

Avec une résolution d’événement désormais extensible.

La réponse de résolution devient exploitable par le futur frontend Vue 3 / TypeScript pour afficher :

- un titre d’événement ;
- une description ;
- un type de résolution ;
- des fragments narratifs ;
- des choix éventuels ;
- un aperçu de récompense ;
- une future transition vers combat, récompense, loi ou narration.

---

## 14. Prochaines étapes recommandées

### Étape 1 — Commit de la session

Commit recommandé :

```bash
git add .
git commit -m "feat(game-engine): add node event resolver pipeline"
git push
```

---

### Étape 2 — Clarifier le legacy backend

Commit recommandé :

```bash
git commit -m "docs(v2): clarify legacy backend status"
```

Objectif : rendre officiel que `backend/` est legacy v1 et que `services/` porte la v2.

---

### Étape 3 — Introduire le choix d’événement

Prochain gros jalon fonctionnel :

```http
POST /api/v2/runs/{runId}/events/{eventId}/choice
```

Objectif : permettre au joueur de prendre une décision à l’intérieur d’un événement.

Cela concernera notamment :

- choisir une récompense ;
- accepter ou refuser une Loi ;
- répondre à un PNJ ;
- acheter ou refuser chez un marchand ;
- accepter ou refuser une malédiction ;
- déclencher une révélation de mémoire.

Commit cible futur :

```bash
git commit -m "feat(game-engine): expose event choice resolution endpoint"
```

---

### Étape 4 — Introduire les premiers effets réels

Une fois le choix d’événement en place, les resolvers devront commencer à appliquer de vraies conséquences :

```text
Rest     → récupération réelle
Item     → récompense proposée ou accordée
Law      → proposition de Loi du Palais
Curse    → coût/risque
Npc      → fragment narratif / choix
Merchant → proposition d’échange
Combat   → création d’une rencontre combat
```

---

### Étape 5 — Introduire le scénario modulaire

Création future des composants :

```text
INarrativeFragmentResolver
EliseDialogueResolver
ScenarioContextBuilder
TomeWriter
```

Objectif : faire du scénario un système serveur-autoritaire, contextualisé par la run et les choix du joueur.

---

## 15. Conclusion

Cette session marque un changement important : le Game Engine ne se contente plus de gérer une carte de nodes. Il commence à interpréter ce que les nodes signifient.

La phrase structurante de cette étape est :

```text
Un node ne doit plus seulement être résolu.
Un node doit produire un outcome.
```

Cette évolution prépare directement :

- le combat ;
- les récompenses ;
- les Lois du Palais ;
- les PNJ ;
- les malédictions ;
- le marchand ;
- Elise ;
- le Tome ;
- le scénario modulaire ;
- l’Event Sourcing métier ;
- la future architecture microservices.

Le projet reste en `alpha0.0.2`, mais son architecture commence désormais à refléter la vision finale.
