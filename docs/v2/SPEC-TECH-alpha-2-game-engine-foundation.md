# L’épopée des silences — SPEC-TECH alpha.2

## Game Engine Foundation

## 1. Objet du document

Ce document formalise l’état d’avancement technique de la refonte v2 de **L’épopée des silences**, anciennement `RPG_ESI07`.

Il couvre le premier socle technique de la branche `v2/develop`, correspondant au jalon :

```text
app-2.0.0-alpha.2 — Socle technique Game Engine
```

L’objectif est de documenter :

* ce qui a été réalisé ;
* pourquoi ces choix ont été faits ;
* quelle architecture a été mise en place ;
* quelles règles techniques encadrent la suite ;
* quelles sont les prochaines étapes.

Ce document sert de support de suivi projet, de mémoire d’architecture et de base de justification technique pour la suite de la v2.

---

## 2. Contexte de la refonte v2

La version v1 de `RPG_ESI07` reposait sur une architecture composée principalement :

* d’un backend ASP.NET Core ;
* d’un frontend Vue 3 ;
* d’un client Unity ;
* d’une base PostgreSQL ;
* d’une logique de sécurité applicative déjà structurée autour de JWT, MFA, audit logs, rate limiting et contrôles d’autorisation.

La v2 marque une rupture produit et technique.

Le projet devient **L’épopée des silences**, un RPG roguelite narratif full web centré sur l’exploration du Palais mental du joueur.

Les décisions structurantes de la v2 sont :

* le client principal devient une application web Vue 3 / TypeScript ;
* Unity devient legacy v1 ;
* le backend devient serveur-autoritaire ;
* le frontend n’envoie que des intentions ;
* les runs sont générées par seed ;
* la génération doit être versionnée ;
* les choix sont visibles et irréversibles ;
* le cœur gameplay est porté par un Game Engine Service ;
* l’Event Sourcing est ciblé en priorité sur les runs ;
* les Lois du Palais doivent être modulaires, extensibles et traçables.

---

## 3. État initial avant le jalon

Avant ce jalon, le dépôt contenait principalement l’architecture v1 :

```text
backend/
frontend/
unity-client/
docs/
.github/
docker-compose.yml
README.md
SECURITY.md
```

Cette architecture n’a pas été supprimée.

La stratégie retenue est de ne pas casser la v1, mais de construire la v2 à côté, dans une structure dédiée.

---

## 4. Branche de travail

La branche de refonte utilisée est :

```text
v2/develop
```

Le nom `develop/v2` n’a pas été utilisé car une branche `develop` existait déjà. Git ne permet pas d’avoir simultanément :

```text
refs/heads/develop
refs/heads/develop/v2
```

La convention retenue pour la v2 est donc :

```text
v2/develop
v2/feature/...
```

---

## 5. Documentation et ADR créées

Une première documentation v2 a été créée dans :

```text
docs/v2/
```

Les ADR suivantes ont été ajoutées :

```text
docs/v2/adr/ADR-001-full-web-client.md
docs/v2/adr/ADR-002-server-authoritative-backend.md
docs/v2/adr/ADR-003-game-engine-service.md
docs/v2/adr/ADR-004-run-event-sourcing.md
```

### ADR-001 — Passage au client full web

Décision :

* Vue 3 / TypeScript devient le client principal ;
* Unity est conservé uniquement comme legacy v1.

Justification :

* accès navigateur immédiat ;
* itération UI plus rapide ;
* meilleure cohérence avec les écrans Tome, run, leaderboard et compagnons ;
* réduction de la dépendance au build Unity.

### ADR-002 — Backend serveur-autoritaire

Décision :

Le backend est source de vérité pour toutes les données critiques.

Le frontend ne transmet que des intentions :

```text
StartRun
ChooseNode
ResolveEventChoice
ChooseCombatAction
SelectReward
AbandonRun
```

Le backend décide :

```text
seed
génération
événements
combat
récompenses
inventaire
progression
score
leaderboard
statut de run
```

Justification :

* limitation de la triche ;
* cohérence du gameplay ;
* auditabilité ;
* base nécessaire à l’Event Sourcing.

### ADR-003 — Game Engine Service central

Décision :

Le cœur gameplay est regroupé dans un Game Engine Service central.

Modules visés :

```text
Run Module
Palace Module
Palace Law Engine
Event Module
Combat Module
Reward Module
Narrative Module
Markov Generator
Tome Writer
```

Justification :

Les règles de run, de génération, d’événements, de récompenses, de narration et de Lois du Palais sont fortement couplées. Une séparation prématurée en microservices augmenterait la complexité sans bénéfice immédiat.

### ADR-004 — Event Sourcing ciblé sur les runs

Décision :

L’Event Sourcing sera appliqué prioritairement au domaine Run.

Justification :

Une run est une séquence de choix irréversibles, d’événements, de récompenses et de transitions. Elle doit pouvoir être reconstruite, auditée et projetée vers :

* l’état courant de run ;
* le Tome ;
* le leaderboard ;
* l’historique joueur ;
* l’audit.

---

## 6. Notice de propriété intellectuelle

Une notice a été ajoutée dans :

```text
docs/legal/IP_NOTICE.md
```

Elle distingue :

* le code source ;
* la documentation technique ;
* l’univers narratif protégé.

Les éléments narratifs suivants restent protégés par droit d’auteur :

```text
L’épopée des silences
Tome des silences
Palais mental
Elise
Neige
Him’Lit
textes narratifs
fragments de lore
dialogues
scénarios
personnages
logos
assets
concepts littéraires issus des livres de l’auteur
```

Cette séparation permet de préparer une ouverture du code tout en protégeant l’univers créatif.

---

## 7. Réorganisation du dépôt

Une structure cible v2 a été préparée :

```text
/
├── apps/
│   ├── web-client/
│   └── admin-portal/
│
├── services/
│   ├── api-gateway/
│   ├── game-engine/
│   ├── identity/
│   ├── catalog/
│   ├── player/
│   ├── leaderboard/
│   └── audit-gdpr/
│
├── packages/
│   ├── shared-contracts/
│   └── shared-kernel/
│
├── infra/
│   ├── docker/
│   └── observability/
│
├── legacy/
│   └── unity-v1/
│
├── tests/
│   ├── backend/
│   ├── integration/
│   └── contract/
│
└── docs/
    └── v2/
```

### Raison de ce découpage

Le dépôt doit permettre de distinguer clairement :

* les applications clientes ;
* les services backend ;
* les packages partagés ;
* l’infrastructure ;
* les tests ;
* les éléments legacy ;
* la documentation de refonte.

### Décision concernant Unity

Le dossier Unity a été déplacé vers :

```text
legacy/unity-v1/
```

Unity est conservé pour :

* historique du projet ;
* consultation ;
* référence technique ;
* éventuelle expérimentation future.

Il ne pilote plus la roadmap v2.

---

## 8. Initialisation du Game Engine Service

Le premier service v2 créé est :

```text
services/game-engine/
```

Une solution dédiée a été créée :

```text
services/game-engine/Leds.GameEngine.slnx
```

La solution est indépendante de la solution v1.

### Projets créés

```text
src/Leds.GameEngine.Api
src/Leds.GameEngine.Application
src/Leds.GameEngine.Domain
src/Leds.GameEngine.Infrastructure

tests/Leds.GameEngine.UnitTests
tests/Leds.GameEngine.IntegrationTests
```

### Raison

Le Game Engine est le cœur de la v2. Il doit être autonome, testable et structuré proprement dès le départ.

---

## 9. Architecture Clean Architecture

Le Game Engine suit une Clean Architecture.

### Domain

Projet :

```text
Leds.GameEngine.Domain
```

Responsabilité :

* entités métier ;
* règles métier pures ;
* value objects ;
* exceptions métier ;
* enums métier.

Le domaine ne dépend d’aucune technologie externe.

Il ne connaît pas :

* ASP.NET Core ;
* EF Core ;
* PostgreSQL ;
* MediatR ;
* Swagger ;
* HTTP.

### Application

Projet :

```text
Leds.GameEngine.Application
```

Responsabilité :

* cas d’usage ;
* commands ;
* queries ;
* handlers ;
* DTOs ;
* interfaces applicatives ;
* validation ;
* orchestration.

Cette couche utilise CQRS avec MediatR.

### Infrastructure

Projet :

```text
Leds.GameEngine.Infrastructure
```

Responsabilité :

* implémentations techniques ;
* génération déterministe ;
* repository temporaire ;
* horloge système ;
* future persistance PostgreSQL/Event Store ;
* future intégration Redis/RabbitMQ si nécessaire.

### Api

Projet :

```text
Leds.GameEngine.Api
```

Responsabilité :

* endpoints HTTP ;
* controllers ;
* middleware ;
* Swagger ;
* configuration ASP.NET Core ;
* injection de dépendances.

Les contrôleurs ne doivent pas contenir de logique métier.

---

## 10. Principes SOLID appliqués

### SRP — Single Responsibility Principle

Chaque classe possède une responsabilité ciblée.

Exemples :

```text
Run
Room
Node
StartRunCommandHandler
DeterministicRunGenerator
InMemoryRunRepository
SystemClock
```

### OCP — Open/Closed Principle

Le générateur, la persistance et l’horloge sont accessibles via interfaces :

```text
IRunGenerator
IRunRepository
IClock
```

Cela permet de remplacer les implémentations sans modifier le handler applicatif.

### LSP — Liskov Substitution Principle

Les implémentations concrètes doivent être substituables par leurs abstractions.

Exemple :

```text
InMemoryRunRepository
```

pourra être remplacé par :

```text
PostgresRunRepository
EventSourcedRunRepository
```

sans modifier `StartRunCommandHandler`.

### ISP — Interface Segregation Principle

Les interfaces sont petites et ciblées.

Exemples :

```text
IClock
IRunGenerator
IRunRepository
```

Aucune interface générique massive de type `IGameEngineService` n’a été créée.

### DIP — Dependency Inversion Principle

La couche Application dépend d’abstractions.

La couche Infrastructure fournit les implémentations concrètes.

---

## 11. Domaine métier créé

Les premiers concepts du domaine ont été créés :

```text
Run
Room
Node
RunStatus
NodeEventType
NodeState
RunId
RoomId
NodeId
DomainException
```

### Run

Une `Run` représente une exploration temporaire du Palais mental.

Elle contient :

```text
RunId
PlayerId
Seed
GeneratorVersion
MarkovMatrixVersion
Status
CurrentRoomId
StartedAt
EndedAt
Rooms
```

Règles implémentées :

* une run appartient à un joueur ;
* une run possède une seed ;
* une run stocke la version du générateur ;
* une run stocke la version de la matrice Markov ;
* une run démarre en statut `Active` ;
* une run démarre avec une room de profondeur 0 ;
* une run démarre avec exactement 4 nœuds disponibles ;
* une run ne peut choisir qu’un nœud disponible ;
* une pièce doit être résolue avant de passer à la suivante ;
* la profondeur maximale est 10 ;
* une run peut être abandonnée.

### Room

Une `Room` représente une pièce du Palais mental.

Elle contient :

```text
RoomId
Depth
Theme
Nodes
```

Règles implémentées :

* profondeur entre 0 et 10 ;
* thème obligatoire ;
* une room contient entre 1 et 70 nœuds ;
* un nœud sélectionné verrouille les autres nœuds de la room.

### Node

Un `Node` représente un choix visible et cliquable.

Il contient :

```text
NodeId
NodeEventType
RiskLevel
RewardProfile
NodeState
```

Règles implémentées :

* risque entre 0 et 100 ;
* profil de récompense obligatoire ;
* seul un nœud disponible peut être sélectionné ;
* seul un nœud sélectionné peut être résolu ;
* les autres nœuds peuvent être verrouillés.

---

## 12. CQRS et cas d’usage StartRun

Le premier cas d’usage applicatif est :

```text
StartRun
```

Fichiers créés :

```text
StartRunCommand
StartRunCommandHandler
StartRunCommandValidator
StartRunResponse
RunDto
RoomDto
NodeDto
```

### Flux applicatif

```text
StartRunCommand
→ StartRunCommandValidator
→ StartRunCommandHandler
→ IRunGenerator.GenerateSeed()
→ IRunGenerator.GenerateInitialRoom(seed)
→ Run.StartNew(...)
→ IRunRepository.AddAsync(run)
→ StartRunResponse
```

### Rôle du handler

`StartRunCommandHandler` orchestre le cas d’usage.

Il ne connaît pas :

* HTTP ;
* PostgreSQL ;
* EF Core ;
* la génération concrète ;
* l’heure système concrète.

Il dépend uniquement de :

```text
IRunGenerator
IRunRepository
IClock
```

---

## 13. Infrastructure minimale

Une infrastructure temporaire a été ajoutée afin de rendre la verticale exécutable.

### SystemClock

Implémentation de :

```text
IClock
```

Rôle :

* fournir l’heure UTC système.

### DeterministicRunGenerator

Implémentation de :

```text
IRunGenerator
```

Rôle :

* générer une seed ;
* générer une room initiale ;
* générer 4 nœuds initiaux.

Versions exposées :

```text
GeneratorVersion = gen-0.1.0
MarkovMatrixVersion = markov-0.1.0
```

La room initiale générée est :

```text
Depth = 0
Theme = Threshold
Nodes = Combat, Memory, Rest, Item
```

Le générateur est déterministe pour une seed donnée concernant :

* les types de nœuds ;
* les niveaux de risque ;
* les profils de récompense.

Limite connue :

Les `RunId`, `RoomId` et `NodeId` sont encore générés par `Guid.NewGuid()`. Ils ne sont donc pas encore déterministes.

Cette limite est acceptée pour `gen-0.1.0`.

### InMemoryRunRepository

Implémentation temporaire de :

```text
IRunRepository
```

Rôle :

* stocker les runs en mémoire ;
* permettre de tester l’API sans PostgreSQL.

Limite connue :

* les données disparaissent au redémarrage de l’application ;
* ce repository n’est pas destiné à la production.

Il sera remplacé plus tard par :

```text
PostgreSQL
Event Store
ou EventSourcedRunRepository
```

---

## 14. Injection de dépendances

Deux extensions DI ont été créées.

### Application

```text
AddGameEngineApplication()
```

Rôle :

* enregistrer MediatR ;
* enregistrer les validators FluentValidation ;
* enregistrer le pipeline de validation.

### Infrastructure

```text
AddGameEngineInfrastructure()
```

Rôle :

* enregistrer `IClock` ;
* enregistrer `IRunGenerator` ;
* enregistrer `IRunRepository`.

Implémentations actuelles :

```text
IClock → SystemClock
IRunGenerator → DeterministicRunGenerator
IRunRepository → InMemoryRunRepository
```

---

## 15. Validation applicative

Un pipeline MediatR de validation a été créé :

```text
ValidationBehavior<TRequest, TResponse>
```

Rôle :

* détecter les validators associés à une command/query ;
* exécuter FluentValidation avant le handler ;
* bloquer l’exécution si la requête est invalide.

Exemple :

```text
StartRunCommandValidator
```

Règle actuelle :

```text
PlayerId obligatoire
```

---

## 16. API exposée

Le premier endpoint HTTP v2 a été ajouté :

```http
POST /api/v2/runs
```

Contrôleur :

```text
RunsController
```

### Requête

```json
{
  "playerId": "11111111-1111-1111-1111-111111111111"
}
```

### Réponse attendue

```http
201 Created
```

Exemple de réponse :

```json
{
  "run": {
    "id": "995ade0e-a468-4c56-a33a-5ded021fbc72",
    "playerId": "11111111-1111-1111-1111-111111111111",
    "seed": "seed-4829d99dbbe84db0940145d04ad76c5e",
    "generatorVersion": "gen-0.1.0",
    "markovMatrixVersion": "markov-0.1.0",
    "status": "Active",
    "currentDepth": 0,
    "currentRoom": {
      "depth": 0,
      "theme": "Threshold",
      "nodes": [
        {
          "eventType": "Combat",
          "riskLevel": 44,
          "rewardProfile": "combat-common",
          "state": "Available"
        },
        {
          "eventType": "Memory",
          "riskLevel": 12,
          "rewardProfile": "narrative",
          "state": "Available"
        },
        {
          "eventType": "Rest",
          "riskLevel": 9,
          "rewardProfile": "none",
          "state": "Available"
        },
        {
          "eventType": "Item",
          "riskLevel": 17,
          "rewardProfile": "common",
          "state": "Available"
        }
      ]
    }
  }
}
```

### Swagger

Swagger est disponible en environnement Development.

Exemple local :

```text
http://localhost:5187/swagger
```

---

## 17. Middleware d’erreurs

Un middleware d’erreurs a été ajouté :

```text
ExceptionHandlingMiddleware
```

Il gère :

```text
ValidationException → 400 Bad Request
DomainException → 400 Bad Request
Exception inconnue → 500 Internal Server Error
```

Les erreurs sont retournées au format :

```text
application/problem+json
```

Objectif :

* éviter les erreurs techniques exposées ;
* harmoniser les réponses ;
* préparer une API propre et sécurisée.

---

## 18. Tests réalisés

Des tests unitaires ont été créés pour le domaine.

### Tests Run

Objectifs vérifiés :

* création d’une run active ;
* obligation d’avoir 4 nœuds initiaux ;
* sélection d’un nœud ;
* verrouillage des autres nœuds ;
* interdiction de sélectionner deux nœuds ;
* résolution d’un nœud sélectionné ;
* passage à la room suivante ;
* abandon d’une run.

### Tests StartRunCommandHandler

Objectifs vérifiés :

* création d’une run ;
* génération de seed ;
* récupération des versions générateur/Markov ;
* persistance via `IRunRepository`.

### Tests DeterministicRunGenerator

Objectifs vérifiés :

* génération d’une room initiale ;
* présence de 4 nœuds ;
* types initiaux attendus ;
* déterminisme des risques et profils de récompense.

### Tests d’intégration API

Objectifs visés :

* `POST /api/v2/runs` retourne `201 Created` si la requête est valide ;
* la réponse contient une run active ;
* la réponse contient 4 nœuds disponibles ;
* un `PlayerId` vide retourne `400 Bad Request`.

---

## 19. Commandes de vérification

Depuis :

```text
services/game-engine/
```

Commandes à utiliser :

```powershell
dotnet build Leds.GameEngine.slnx
dotnet test Leds.GameEngine.slnx
dotnet run --project src/Leds.GameEngine.Api/Leds.GameEngine.Api.csproj
```

Test manuel API :

```http
POST http://localhost:5187/api/v2/runs
Content-Type: application/json

{
  "playerId": "11111111-1111-1111-1111-111111111111"
}
```

---

## 20. Commits significatifs

Commits principaux du jalon :

```text
docs(v2): add initial refactor architecture decisions
chore(v2): prepare repository structure and isolate Unity legacy
feat(game-engine): initialize clean architecture solution
feat(game-engine): add run room node domain model
feat(game-engine): add start run application use case
feat(game-engine): add minimal infrastructure services
feat(game-engine): expose start run api endpoint
```

---

## 21. Limites connues

### Persistance temporaire

Le repository actuel est en mémoire.

Conséquences :

* pas de persistance réelle ;
* perte des données au redémarrage ;
* pas encore d’Event Store ;
* pas encore de reconstruction de run.

### IDs non déterministes

Les IDs de run, room et node sont encore générés via `Guid.NewGuid()`.

Conséquences :

* la structure logique est reproductible ;
* les identifiants ne le sont pas encore.

### Authentification non branchée

Le endpoint `POST /api/v2/runs` accepte actuellement un `PlayerId` dans le body.

Ce choix est temporaire.

À terme :

* le `PlayerId` devra venir du JWT ;
* le frontend ne devra pas pouvoir choisir arbitrairement un joueur ;
* les ownership checks seront obligatoires.

### Pas encore de ChooseNode

La run peut être créée, mais le joueur ne peut pas encore choisir un nœud via API.

Le domaine supporte déjà partiellement cette règle, mais le cas d’usage applicatif et l’endpoint ne sont pas encore exposés.

### Pas encore d’Event Store

L’Event Sourcing est documenté mais pas encore implémenté.

Il faudra créer :

```text
RunEvents
IRunEventStore
AppendAsync
Rehydrate
Sequence
CorrelationId
CausationId
```

### Pas encore de client web v2

Le client Vue 3 v2 n’est pas encore initialisé dans `apps/web-client`.

---

## 22. Bilan du jalon

Le jalon `alpha.2 — Game Engine Foundation` pose une première fondation solide.

La verticale suivante est désormais fonctionnelle :

```text
HTTP API
→ Controller
→ MediatR
→ ValidationBehavior
→ StartRunCommandHandler
→ IRunGenerator
→ Domaine Run / Room / Node
→ IRunRepository
→ Réponse HTTP 201 Created
```

Cette verticale valide les choix d’architecture initiaux :

* Clean Architecture ;
* CQRS ;
* séparation des responsabilités ;
* backend serveur-autoritaire ;
* domaine métier isolé ;
* infrastructure remplaçable ;
* tests unitaires et intégration ;
* documentation ADR.

---

## 23. Prochaines étapes recommandées

### Étape 1 — Finaliser `GET /api/v2/runs/{runId}`

Objectif :

* récupérer l’état courant d’une run ;
* préparer l’affichage frontend ;
* ajouter `IRunRepository.GetByIdAsync`.

### Étape 2 — Implémenter `ChooseNode`

Objectif :

* exposer `POST /api/v2/runs/{runId}/nodes/{nodeId}/choose` ;
* sélectionner un nœud ;
* verrouiller les autres ;
* retourner l’état mis à jour de la run.

### Étape 3 — Ajouter l’Event Store minimal

Objectif :

* créer les événements initiaux ;
* persister `RunStarted`, `RoomGenerated`, `NodeSelected` ;
* préparer la reconstruction d’état.

### Étape 4 — Remplacer progressivement le repository in-memory

Objectif :

* utiliser PostgreSQL ;
* préparer l’Event Store append-only ;
* conserver l’in-memory uniquement pour les tests.

### Étape 5 — Initialiser le client web v2

Objectif :

* créer `apps/web-client` ;
* afficher une run ;
* afficher les 4 nœuds initiaux ;
* préparer le choix de nœud.

---

## 24. Règle de conduite pour la suite

La suite du développement doit respecter les principes suivants :

```text
Ne pas casser la v1.
Ne pas dépendre de Unity legacy.
Garder le domaine indépendant des frameworks.
Ajouter les règles métier dans Domain.
Orchestrer les cas d’usage dans Application.
Isoler les détails techniques dans Infrastructure.
Garder les contrôleurs API minces.
Tester chaque règle importante.
Documenter chaque décision structurante.
Commiter petit et vérifiable.
```

---

## 25. Conclusion

Ce jalon marque le début concret de la v2.

Le projet dispose maintenant d’une base technique propre, cohérente et extensible pour construire progressivement le cœur du gameplay de **L’épopée des silences**.

La prochaine priorité est de permettre au joueur de consulter une run existante, puis de choisir un nœud, afin de transformer la création de run en première boucle jouable minimale.
