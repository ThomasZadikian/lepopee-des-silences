# Architecture système — alpha-0.5

Ce document est la carte complète du projet L'épopée des silences. Il décrit ce qui existe, pourquoi cela existe, où se trouvent les éléments, comment les services sont séparés, comment les flux fonctionnent, quelles sont les prochaines étapes et les limites connues.

---

## 1. Objectif du document

Ce document sert de référence complète pour comprendre le projet sans avoir à reconstituer mentalement toute l'architecture. Il couvre les microservices Game Engine, Catalog, Player, le frontend web-client, la persistance PostgreSQL, l'environnement Docker local et les flux de bout en bout.

Il est destiné au porteur du projet, aux développeurs qui reprennent le code et aux outils d'automatisation qui doivent naviguer le codebase de manière déterministe.

---

## 2. Vue d'ensemble du projet

L'épopée des silences est un RPG roguelite narratif full web. Le joueur explore son propre Palais mental à travers des runs procédurales reproductibles par seed. Chaque run représente une exploration temporaire avec des choix irréversibles, une progression roguelite et un chapitre écrit dans le Tome du joueur.

Le client web transmet uniquement des intentions. Le backend décide des résultats critiques : génération, événements, combats, récompenses, progression, score et état de run. L'architecture est serveur-autoritaire.

Le projet est en architecture microservices avec séparation claire entre :
- les définitions stables (Catalog Service)
- les données permanentes du joueur (Player Service)
- le runtime de run (Game Engine Service)
- l'interface utilisateur (web-client)

---

## 3. État actuel des versions

| Composant | Version | État |
|---|---|---|
| Game Engine Service | alpha-0.5.0-dev.7 | Persistance relationnelle active, intégration Player Service |
| Player Service | alpha-0.5.4 | PostgreSQL persistence active, EF Core |
| Catalog Service | alpha-0.0.4 | Stable pour contrats actuels, InMemory |
| Frontend web-client | alpha-0.4.0 | Combat → reward → progression fonctionnel |
| PostgreSQL Game Engine | Actif | localhost:5432 |
| PostgreSQL Player | Prêt | localhost:5433 |

---

## 4. Vue d'ensemble microservices

```
┌─────────────────┐
│   web-client     │ http://localhost:5173
│   (Vue 3/Vite)   │
└────────┬────────┘
         │
         ▼
┌─────────────────────────┐
│   Game Engine Service    │ http://localhost:5187
│   Runtime de run         │
└──────────┬──────────────┘
           │                        ┌───────────────────┐
           ├───────────────────────▶│ Catalog Service    │ http://localhost:5193
           │  définitions stables   │ Définitions        │
           │                        └───────────────────┘
           │
           │                        ┌───────────────────┐
           └───────────────────────▶│ Player Service     │ http://localhost:5189
              snapshot joueur        │ Profil permanent   │
                                     └───────────────────┘

┌──────────────────────┐  ┌──────────────────────┐
│ PostgreSQL Game Engine│  │ PostgreSQL Player     │
│ localhost:5432        │  │ localhost:5433        │
│ leds_game_engine      │  │ leds_player           │
└──────────────────────┘  └──────────────────────┘
```

Le Catalog Service ne possède pas encore de PostgreSQL. Ses définitions sont exposées via des endpoints HTTP et consommées par le Game Engine.

---

## 5. Règle de séparation des responsabilités

| Bounded context | Responsabilité | Service |
|---|---|---|
| Données permanentes du joueur | PlayerProfile, PlayerCharacter, roster, progression hors run, unlocks | Player Service |
| Définitions stables | Skill definitions, enemy definitions, item definitions, palace law definitions, boss definitions, reward templates | Catalog Service |
| Runtime de run | Run, rooms, map nodes, combats actifs, rewards runtime, snapshot joueur pendant une run, progression pendant une run | Game Engine Service |
| Affichage et interaction | Interface utilisateur, transmission d'intentions | web-client |

Règles fondamentales :
- Permanent = Player Service
- Runtime de run = Game Engine Service
- Définitions = Catalog Service
- Affichage/interaction = web-client

Aucune logique permanente joueur ne doit vivre dans Game Engine. Aucune logique runtime de run ne doit vivre dans Player. Aucune définition stable ne doit être hardcodée dans Game Engine si elle appartient au Catalog.

---

## 6. Cartographie globale du repository

```
rpg_esi07/
├── apps/
│   └── game-client/              # Frontend Vue 3
├── services/
│   ├── game-engine/              # Game Engine Service
│   ├── catalog/                  # Catalog Service
│   └── player/                   # Player Service
├── packages/
│   └── shared-building-blocks/   # Partagé (IClock, Result, Error)
├── legacy/                       # V1 conservée (Unity + backend monolithique)
├── docs/                         # Documentation
├── scripts/
│   └── dev/                      # Scripts d'orchestration locale
├── docker-compose.dev.yml        # Docker PostgreSQL local
├── docker-compose.yml            # Docker PostgreSQL Game Engine seul
├── .env.example                  # Variables d'environnement
└── README.md                     # Documentation racine
```

---

## 7. Game Engine Service

### 7.1 Rôle du service

Le Game Engine est le cœur du projet. Il gère tout le runtime d'une run : génération de contenu, exploration de map, combats au tour par tour, récompenses, progression et état du joueur pendant la run.

### 7.2 Ce que le service possède

- L'état runtime d'une run (Run, Room, MapNode)
- Le combat actif (Combat, Combatant, CombatantSkill)
- Les rewards runtime (RewardOffer, RewardChoice)
- Le snapshot joueur pendant la run (PlayerRuntimeState)
- Les lois actives pendant une run (ActivePalaceLaw)
- Les décisions de progression pendant une run
- La génération déterministe de contenu (rooms, maps, encounters)

### 7.3 Ce que le service ne doit pas posséder

- Le profil joueur permanent
- Les personnages persistants du joueur
- La progression hors run
- Les définitions stables de skills/enemies/items
- L'inventaire du joueur
- L'économie du jeu

### 7.4 Structure de dossiers

```
services/game-engine/
├── src/
│   ├── Leds.GameEngine.Domain/         # Agrégats, value objects, enums
│   ├── Leds.GameEngine.Application/    # Commands, queries, handlers, gateways
│   ├── Leds.GameEngine.Infrastructure/ # EF Core, repositories, gateways HTTP
│   └── Leds.GameEngine.Api/            # Controllers, DI, configuration
├── tests/
│   ├── Leds.GameEngine.UnitTests/      # 61 fichiers de tests unitaires
│   └── Leds.GameEngine.IntegrationTests/ # 19 fichiers de tests d'intégration
└── Leds.GameEngine.slnx
```

### 7.5 Projets .NET

| Projet | Rôle | Framework |
|---|---|---|
| `Leds.GameEngine.Domain` | Agrégats, value objects, enums, logique métier | net10.0 |
| `Leds.GameEngine.Application` | Use cases CQRS, interfaces, gateways abstraits | net10.0 |
| `Leds.GameEngine.Infrastructure` | EF Core, repositories, gateways HTTP/InMemory | net10.0 |
| `Leds.GameEngine.Api` | Controllers, middleware, DI composition root | net10.0 |
| `Leds.GameEngine.UnitTests` | Tests unitaires (xUnit, Moq, FluentAssertions) | net10.0 |
| `Leds.GameEngine.IntegrationTests` | Tests d'intégration (WebApplicationFactory) | net10.0 |

Dépendances partagées : `Leds.SharedBuildingBlocks` (IClock, Result, Error).

### 7.6 Layer Domain

Le Domain ne dépend d'aucun autre projet. Il contient les agrégats et la logique métier pure.

59 fichiers organisés en 10 sous-dossiers :
- `Combats/` (15 fichiers) — Combat, Combatant, CombatantSkill, enums
- `Runs/` (5 fichiers) — Run, PlayerRuntimeState, PlayerRuntimeSkill
- `Rooms/` (5 fichiers) — Room, RoomBossProfile, RoomType, RoomState
- `Nodes/` (4 fichiers) — MapNode, Node, NodeId, NodeState
- `NodeEvents/` (3 fichiers) — NodeEvent, NodeEventStatus, NodeEventType
- `Rewards/` (7 fichiers) — RewardOffer, RewardChoice, RewardSource, RewardType
- `PalaceLaws/` (4 fichiers) — PalaceLaw, ActivePalaceLaw
- `Common/` (5 fichiers) — DomainException, CombatInstance (legacy)
- `Markov/` (7 fichiers) — Matrices de Markov pour génération déterministe
- `Interlude/` (3 fichiers) — InterludeNode, InterludeNodeType

### 7.7 Agrégat Run

Fichier : `services/game-engine/src/Leds.GameEngine.Domain/Runs/Run.cs.cs`

`Run` est l'agrégat racine. Il contient :
- Identité : `RunId`, `Guid PlayerId`, `string Seed`
- Statut : `RunStatus` (Active, RoomResolved, Completed, Failed, Abandoned, Suspended, Interlude)
- Progression : `CurrentRoomIndex`, `CurrentRoomId`, `IReadOnlyCollection<Room> Rooms`
- Combat actif : `CombatId? ActiveCombatId`, `Combat? ActiveCombat`
- Reward pending : `RewardOfferId? PendingRewardOfferId`
- Joueur runtime : `PlayerRuntimeState PlayerState`
- Stats legacy : `MaxHp`, `CurrentHp`, `Attack`, `Defense`, `Speed`
- Snapshot : `_roomSnapshot` (pour rollback ExitMidRoom), `_preSuspendStatus` (pour Resume)
- Collections privées : `_rooms`, `_activePalaceLaws`, `_memoryFragments`

Méthodes publiques contrôlées : `StartNew`, `Rehydrate`, `StartCombat`, `CompleteActiveCombat`, `FailActiveCombat`, `ChooseNode`, `ResolveCurrentEvent`, `ApplyReward`, `ApplyHeal`, `SaveAndExit`, `Resume`, `ExitMidRoom`.

### 7.8 Room et MapNode

**Room** (`services/game-engine/src/Leds.GameEngine.Domain/Rooms/Room.cs`) :
- `RoomId`, `int Depth`, `RoomType`, `string Theme`, `RoomBossProfile`
- `RoomState` (Active, NodeSelected, NodeResolved, BossReached, Completed)
- `CurrentNodeDepth`, `MaxNodeDepth`, `IReadOnlyCollection<MapNode> Nodes`
- Gère la progression du joueur à travers les lignes de nodes

**MapNode** (`services/game-engine/src/Leds.GameEngine.Domain/Nodes/MapNode.cs`) :
- `NodeId`, `NodeEventType`, `int Row`, `int Lane`, `int RiskLevel`
- `string RewardProfile`, `bool IsBoss`, `NodeState State`
- `IReadOnlyCollection<NodeId> ParentNodeIds` — graphe DAG de la carte
- `string? ChosenEventOptionId`

MapNode est le modèle runtime réel utilisé par Room. Il a un `EventType` direct (pas de NodeEvent enfants). Le Node plus riche avec ses NodeEvent enfants existe dans le code mais n'est pas utilisé par l'agrégat Run actuel.

### 7.9 Combat runtime

**Combat** (`services/game-engine/src/Leds.GameEngine.Domain/Combats/Combat.cs`) :
- `CombatId`, `RunId`, `RoomId`, `NodeId`
- `CombatStatus` (Pending, Active, Completed, Failed)
- `IReadOnlyCollection<Combatant> Allies`, `IReadOnlyCollection<Combatant> Enemies`
- `CombatantId? ActiveCombatantId`, `int TurnNumber`

**Combatant** (`services/game-engine/src/Leds.GameEngine.Domain/Combats/Combatant.cs`) :
- `CombatantId`, `string SourceKey`, `string DisplayName`, `CombatantSide Side`
- `int MaxVitality`, `int CurrentVitality`, `int Guard`, `int Mana`, `int Charge`
- `CombatantStatus Status`, `IReadOnlyCollection<CombatantSkill> Skills`

**CombatantSkill** (`services/game-engine/src/Leds.GameEngine.Domain/Combats/CombatantSkill.cs`) :
- `string Key`, `string DisplayName`, `string SkillType`, `string TargetingType`, `string EffectType`
- `int ManaCost`, `int ChargeCost`, `int BasePower`

### 7.10 Reward runtime

**RewardOffer** (`services/game-engine/src/Leds.GameEngine.Domain/Rewards/RewardOffer.cs`) :
- `RewardOfferId`, `RewardSource`, `RewardOfferState`, `RewardChoiceId? SelectedChoiceId`
- `IReadOnlyCollection<RewardChoice> Choices`

**RewardChoice** (`services/game-engine/src/Leds.GameEngine.Domain/Rewards/RewardChoice.cs`) :
- `RewardChoiceId`, `RewardType`, `string Label`, `string Description`, `string PayloadKey`

`RewardType` : Heal, TemporaryItem, StatBonus, MemoryFragment.

### 7.11 Player snapshot runtime

**PlayerRuntimeState** (`services/game-engine/src/Leds.GameEngine.Domain/Runs/PlayerRuntimeState.cs`) :
- `int MaxVitality`, `int CurrentVitality`, `int Guard`, `int Mana`, `int Charge`
- `IReadOnlyCollection<PlayerRuntimeSkill> Skills`
- Méthodes : `TakeDamage`, `Heal`, `GainGuard`, `SpendMana`, `GainMana`, `SpendCharge`, `GainCharge`, `SyncFromCombat`

**PlayerRuntimeSkill** (`services/game-engine/src/Leds.GameEngine.Domain/Runs/PlayerRuntimeSkill.cs`) :
- `string Key`, `string DisplayName`, `string SkillType`, `string TargetingType`, `string EffectType`
- `int ManaCost`, `int ChargeCost`, `int BasePower`

### 7.12 Layer Application

96 fichiers organisés en feature folders. Chaque use case suit le pattern CQRS MediatR :
- `Command` (sealed record : IRequest)
- `CommandHandler` (sealed class : IRequestHandler)
- `CommandValidator` (sealed class : AbstractValidator)
- `Response` (sealed record)

Features principales :
- `Runs/` : StartRun, ChooseNode, ResolveCurrentEvent, ProgressRun, UseCombatSkill, GetCurrentCombat, GetRunById, AbandonRun, SaveAndExitRun, ResumeRun, ExitMidRoom, MoveToNextRoom
- `Combats/` : SubmitCombatAction (legacy), CombatFactory, CombatRiskProfileResolver, CombatSkillEffectResolver, EnemyCombatTurnResolver
- `Events/` : ChooseEventOption, ResolveNodeEvent, 10+ resolver strategies
- `Rewards/` : SelectReward, RewardOfferFactory
- `Players/` : IPlayerRunSnapshotGateway, PlayerRunSnapshot

Interfaces dans `Application/Abstractions/` :
- `IRunRepository` — AddAsync, GetByIdAsync, UpdateAsync
- `IRunGenerator` — GenerateSeed, GenerateInitialRoomAsync
- `ICombatFactory` — CreateFromDraft
- `ICatalogContentGateway` — GetEnemyTemplateByKeyAsync, GetSkillTemplateByKeyAsync, etc.
- `IPlayerRunSnapshotGateway` — GetRunSnapshotAsync
- `IRewardOfferRepository` — AddAsync, GetByIdAsync, UpdateAsync

### 7.13 Commands et Queries principales

| Command/Query | Handler | Rôle |
|---|---|---|
| `StartRunCommand` | `StartRunCommandHandler` | Démarrer une run, récupérer snapshot joueur |
| `ChooseNodeCommand` | `ChooseNodeCommandHandler` | Choisir un node sur la carte |
| `ResolveCurrentEventCommand` | `ResolveCurrentEventCommandHandler` | Résoudre l'événement du node sélectionné |
| `UseCombatSkillCommand` | `UseCombatSkillCommandHandler` | Utiliser une skill en combat |
| `GetCurrentCombatQuery` | `GetCurrentCombatHandler` | Récupérer le combat actif |
| `GetRunByIdQuery` | `GetRunByIdHandler` | Récupérer une run par ID |
| `SelectRewardCommand` | `SelectRewardCommandHandler` | Sélectionner une récompense |
| `ProgressRunCommand` | `ProgressRunCommandHandler` | Avancer la progression de la run |
| `AbandonRunCommand` | `AbandonRunCommandHandler` | Abandonner la run |
| `SaveAndExitRunCommand` | `SaveAndExitRunCommandHandler` | Sauvegarder et quitter |
| `ResumeRunCommand` | `ResumeRunCommandHandler` | Reprendre une run suspendue |

### 7.14 Gateways sortantes

| Gateway | Interface | Implémentation InMemory | Implémentation HTTP |
|---|---|---|---|
| Catalog | `ICatalogContentGateway` | `InMemoryCatalogContentGateway` | `HttpCatalogContentGateway` |
| Player | `IPlayerRunSnapshotGateway` | `InMemoryPlayerRunSnapshotGateway` | `HttpPlayerRunSnapshotGateway` |

### 7.15 Layer Infrastructure

80 fichiers organisés en :
- `Persistence/` (39 fichiers) — DbContext, entities, configurations, mappers, repositories, migrations
- `Generation/` (15 fichiers) — Génération déterministe de rooms, maps, types, thèmes
- `Catalog/` (6 fichiers) — Gateway HTTP/InMemory vers Catalog
- `Players/` (3 fichiers) — Gateway HTTP/InMemory vers Player
- `Combats/` (5 fichiers) — Validation, composition, draft, targeting
- `Events/Resolutions/` (8 fichiers) — Stratégies de résolution d'événements
- `Clock/` (1 fichier) — IClock implementation
- `Rewards/` (1 fichier) — InMemoryRewardOfferRepository

### 7.16 Repositories

| Interface | Implémentation InMemory | Implémentation EF |
|---|---|---|
| `IRunRepository` | `InMemoryRunRepository` | `EfRunRepository` |
| `ICombatInstanceRepository` | `InMemoryCombatInstanceRepository` | — |
| `IRewardOfferRepository` | `InMemoryRewardOfferRepository` | — |

`EfRunRepository` charge et sauvegarde l'agrégat complet via Include chainés et mappers dédiés.

### 7.17 EF Core / PostgreSQL

- Provider : `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.2
- DbContext : `GameEngineDbContext` dans `Infrastructure/Persistence/GameEngineDbContext.cs`
- Design-time factory : `GameEngineDbContextFactory`
- 12 DbSets : Runs, Rooms, MapNodes, MapNodeParentNodes, RunMemoryFragments, RunActivePalaceLaws, Combats, Combatants, CombatantSkills, PlayerRuntimeStates, PlayerRuntimeSkills

Configuration DI conditionnelle :
- `Persistence:Mode = InMemory` → `InMemoryRunRepository` (défaut)
- `Persistence:Mode = Postgres` → `EfRunRepository` + `GameEngineDbContext`

### 7.18 Entités persistées

| Entity EF | Table | Description |
|---|---|---|
| `RunEntity` | `runs` | Identité, statut, progression, HP, stats, snapshot |
| `RoomEntity` | `run_rooms` | Rooms avec boss profile |
| `MapNodeEntity` | `run_nodes` | Nodes avec positions, états |
| `MapNodeParentNodeEntity` | `run_node_parent_nodes` | Relations parent/enfant M:N |
| `RunMemoryFragmentEntity` | `run_memory_fragments` | Fragments collectés |
| `RunActivePalaceLawEntity` | `run_active_palace_laws` | Lois actives |
| `CombatEntity` | `run_active_combats` | Combat actif |
| `CombatantEntity` | `run_combatants` | Combattants alliés/ennemis |
| `CombatantSkillEntity` | `run_combatant_skills` | Skills des combattants |
| `PlayerRuntimeStateEntity` | `run_player_states` | État vitalité/guard/mana/charge |
| `PlayerRuntimeSkillEntity` | `run_player_skills` | Skills du joueur |

3 mappers dédiés :
- `RunPersistenceMapper` — Run ↔ RunEntity
- `CombatPersistenceMapper` — Combat ↔ CombatEntity
- `PlayerRuntimeStatePersistenceMapper` — PlayerRuntimeState ↔ PlayerRuntimeStateEntity

### 7.19 Migrations

4 migrations dans `Infrastructure/Persistence/Migrations/` :
1. `InitialGameEnginePersistence` — table `runs`
2. `AddRunMapStatePersistence` — tables `run_rooms`, `run_nodes`, `run_node_parent_nodes`, `run_memory_fragments`, `run_active_palace_laws`
3. `AddActiveCombatPersistence` — tables `run_active_combats`, `run_combatants`, `run_combatant_skills`
4. `AddPlayerRuntimeStatePersistence` — tables `run_player_states`, `run_player_skills`

### 7.20 Layer API

12 fichiers source :
- `Program.cs` — composition root
- `Controllers/RunsController.cs` — endpoints runs
- `Controllers/CombatsController.cs` — endpoints combats
- `Controllers/RewardsController.cs` — endpoints rewards
- `Controllers/InterludeController.cs` — endpoints interlude
- `Middleware/ExceptionHandlingMiddleware.cs` — gestion centralisée des erreurs

### 7.21 Endpoints principaux

| Méthode | Route | Handler |
|---|---|---|
| POST | `/api/v2/runs` | `StartRunCommandHandler` |
| GET | `/api/v2/runs/{runId}` | `GetRunByIdHandler` |
| POST | `/api/v2/runs/{runId}/nodes/{nodeId}/choose` | `ChooseNodeCommandHandler` |
| POST | `/api/v2/runs/{runId}/current-event/resolve` | `ResolveCurrentEventCommandHandler` |
| POST | `/api/v2/runs/{runId}/progress` | `ProgressRunCommandHandler` |
| GET | `/api/v2/runs/{runId}/current-combat` | `GetCurrentCombatHandler` |
| POST | `/api/v2/runs/{runId}/combats/{combatId}/skill-actions` | `UseCombatSkillCommandHandler` |
| GET | `/api/v2/runs/{runId}/rewards/pending` | `RewardsController.GetPendingReward` |
| POST | `/api/v2/runs/{runId}/rewards/select` | `SelectRewardCommandHandler` |
| POST | `/api/v2/runs/{runId}/abandon` | `AbandonRunCommandHandler` |
| POST | `/api/v2/runs/{runId}/save-and-exit` | `SaveAndExitRunCommandHandler` |
| POST | `/api/v2/runs/{runId}/resume` | `ResumeRunCommandHandler` |
| POST | `/api/v2/runs/{runId}/exit-mid-room` | `ExitMidRoomCommandHandler` |
| POST | `/api/v2/runs/{runId}/rooms/next` | `MoveToNextRoomCommandHandler` |
| POST | `/api/v2/runs/{runId}/interlude/enter` | `EnterInterludeCommandHandler` |
| GET | `/api/v2/runs/{runId}/interlude` | `GetInterludeQueryHandler` |

### 7.22 Tests importants

**UnitTests** (835 tests, 61 fichiers) :
- `StartRunCommandHandlerTests` — démarrage de run avec snapshot joueur
- `UseCombatSkillCommandHandlerTests` — actions combat, tours ennemis, rewards
- `ResolveCurrentEventCommandHandlerTests` — résolution d'événements
- `SelectRewardCommandHandlerTests` — sélection de rewards
- `PlayerRuntimeStateTests` — état joueur, dégâts, heal, sync
- `RunTests` — invariants de l'agrégat Run
- `CombatTests`, `CombatantTests` — invariants combat

**IntegrationTests** (69 tests, 19 fichiers) :
- `StartRunEndpointTests` — démarrage via API
- `CombatActionEndpointTests` — actions combat via API
- `CombatFullFlowEndpointTests` — combat complet victoire
- `EfRunRepositoryTests` — round-trip persistence
- `EfRunRepositoryCombatTests` — round-trip combat
- `EfRunRepositoryPlayerStateTests` — round-trip player state

### 7.23 Flux StartRun

1. Frontend appelle `POST /api/v2/runs` avec `{ playerId }`.
2. `StartRunCommandHandler` injecte `IPlayerRunSnapshotGateway`.
3. Le handler appelle `GetRunSnapshotAsync(playerId)`.
4. Le gateway retourne un `PlayerRunSnapshot` (InMemory ou HTTP vers Player Service).
5. Le handler extrait le personnage principal et ses skills.
6. `Run.StartNew(...)` crée la run avec `PlayerRuntimeState` initialisé depuis le snapshot.
7. `IRunGenerator.GenerateInitialRoomAsync(seed)` génère la première salle.
8. La run est persistée via `IRunRepository`.
9. L'API retourne `RunDto` avec les rooms, nodes et état joueur.

### 7.24 Flux map / node

1. La run démarre avec une première `Room` contenant des `MapNode` en grille (rows × lanes).
2. Le joueur choisit un node disponible → `ChooseNode` → node passe en `Selected`.
3. `ResolveCurrentEvent` résout l'événement du node.
4. Selon le type (Combat, Item, NPC, Rest, Law, Merchant, Rare, Elite, RoomBoss), le contenu approprié est chargé.
5. Pour un node combat : un `Combat` est créé et attaché à la run.
6. Pour un node item/npc/law : un outcome est retourné.
7. Après résolution, le node passe en `Resolved` et les nodes suivants deviennent `Available`.
8. Quand le boss node est résolu, la room passe en `Completed` → `RoomResolved`.

### 7.25 Flux combat

1. `ResolveCurrentEventCommandHandler` identifie un node de type combat.
2. `CombatEncounterDraftGenerator` génère un draft avec les ennemis du Catalog.
3. `CombatFactory.CreateFromDraft(draft, playerState)` crée le combat avec les stats et skills du joueur depuis `PlayerRuntimeState`.
4. `Run.StartCombat(combat)` attache le combat à la run.
5. Le frontend affiche le combat via `GET /current-combat`.
6. Le joueur sélectionne une skill et une cible.
7. `POST /skill-actions` envoie l'action au backend.
8. `UseCombatSkillCommandHandler` résout l'effet, synchronise le `PlayerRuntimeState` depuis le combattant joueur.
9. Les tours ennemis sont résolus automatiquement.
10. Quand tous les ennemis sont vaincus → combat `Completed` → `RewardOffer` créé.
11. Quand tous les alliés sont vaincus → combat `Failed` → run `Failed`.

### 7.26 Flux reward

1. Après victoire, `UseCombatSkillCommandHandler` crée un `RewardOffer` via `RewardOfferFactory`.
2. Le `RewardOffer` est persisté et attaché à la run via `PendingRewardOfferId`.
3. Le frontend affiche la page de récompense via `GET /rewards/pending`.
4. Le joueur sélectionne une option.
5. `POST /rewards/select` → `SelectRewardCommandHandler`.
6. `Run.ApplyReward(choice)` applique l'effet (ex: heal sur `PlayerRuntimeState`).
7. `PendingRewardOfferId` est nettoyé.
8. La run peut continuer.

### 7.27 Intégration Catalog

Game Engine accède au Catalog via `ICatalogContentGateway` :

**Mode InMemory** (défaut) : contenu hardcodé dans `InMemoryCatalogContentGateway` avec enemy templates, skill definitions, palace law definitions, room boss profiles.

**Mode Http** : appels HTTP au Catalog Service (`localhost:5193`). Configuration :
```json
{
  "CatalogGateway": {
    "Mode": "Http",
    "BaseUrl": "http://localhost:5193",
    "Timeout": "00:00:05"
  }
}
```

### 7.28 Intégration Player

Game Engine accède au Player Service via `IPlayerRunSnapshotGateway` :

**Mode InMemory** (défaut) : snapshot de développement stable (DisplayName "Joueur", character "Le Porteur", MaxVitality 100, skills skill.basic.strike + skill.basic.guard).

**Mode Http** : `GET /api/v2/players/{playerId}/run-snapshot`. Configuration :
```json
{
  "PlayerGateway": {
    "Mode": "Http",
    "BaseUrl": "http://localhost:5189",
    "Timeout": "00:00:05"
  }
}
```

Flux : StartRun récupère le snapshot une seule fois. Le snapshot est copié dans `PlayerRuntimeState`. Game Engine ne rappelle pas Player pendant la run.

### 7.29 Limites actuelles du Game Engine

- RewardOffers persistées en mémoire seulement (pas encore en PostgreSQL)
- Legacy `CombatInstance` coexiste avec le nouveau système `Combat`
- Catalog gateway partiellement implémenté en HTTP (certaines méthodes throw)
- Pas de Rest Node, Law Node, Merchant Node
- Pas de compagnons ou party runtime
- Pas d'inventaire ou économie

---

## 8. Catalog Service

### 8.1 Rôle du service

Le Catalog fournit les définitions stables utilisées par Game Engine pour générer le contenu des runs. Il est la source de vérité pour les templates de skills, d'ennemis, d'items, de boss, de lois et d'événements.

### 8.2 Ce que le service possède

- Skill templates et definitions
- Enemy templates et definitions
- Item templates
- Event templates
- Room boss definitions
- Palace law definitions

### 8.3 Ce que le service ne doit pas posséder

- État runtime d'une run
- Données joueur
- Progression
- Combats
- Rewards runtime

### 8.4 Structure de dossiers

```
services/catalog/
├── src/
│   ├── Leds.Catalog.Domain/         # Entities Catalog, abstractions
│   ├── Leds.Catalog.Application/    # Queries, handlers, DTOs
│   ├── Leds.Catalog.Infrastructure/ # InMemory read stores
│   └── Leds.Catalog.Api/            # Controllers, middleware
├── tests/
│   ├── Leds.Catalog.UnitTests/
│   └── Leds.Catalog.IntegrationTests/
└── Leds.Catalog.slnx
```

### 8.5 Domain

37 fichiers avec des interfaces et implementations pour :
- `SkillTemplate`, `SkillDefinition` (Key, DisplayName, SkillType, TargetingType, EffectType, ManaCost, ChargeCost, BasePower)
- `EnemyTemplate`, `EnemyDefinition` (Key, DisplayName, Archetype, BaseDifficulty, Skills)
- `ItemTemplate` (Key, DisplayName, Category, Rarity, Duration)
- `EventTemplate` (Key, DisplayName, Type, OutcomeKind)
- `RoomBossDefinition` (Key, Name, RoomType, EnemyTemplateKey)
- `PalaceLawDefinition` (Key, Name, Description, Visibility, ImpactDomain)

### 8.6 Définitions Catalog

Catalog expose des endpoints de lecture uniquement (GET). Chaque type de définition a :
- Un endpoint GET `/api/v2/catalog/{resource}` pour lister les actifs
- Un endpoint GET `/api/v2/catalog/{resource}/{key}` pour obtenir par clé
- Des endpoints de filtrage supplémentaires selon le type

### 8.7 Endpoints exposés

| Controller | Route | Description |
|---|---|---|
| `SkillTemplatesController` | `/api/v2/catalog/skills` | Templates de skills |
| `SkillDefinitionsController` | `/api/v2/catalog/skill-definitions` | Définitions de skills |
| `EnemyTemplatesController` | `/api/v2/catalog/enemies` | Templates d'ennemis |
| `EnemyDefinitionsController` | `/api/v2/catalog/enemy-definitions` | Définitions d'ennemis |
| `ItemTemplatesController` | `/api/v2/catalog/items` | Templates d'items |
| `EventTemplatesController` | `/api/v2/catalog/event-templates` | Templates d'événements |
| `RoomBossDefinitionsController` | `/api/v2/catalog/room-boss-definitions` | Définitions de boss |
| `PalaceLawDefinitionsController` | `/api/v2/catalog/palace-laws` | Définitions de lois |

### 8.8 Relation avec Game Engine

Game Engine consomme Catalog via `ICatalogContentGateway`. Catalog ne connaît pas les runs, les combats ou les joueurs.

### 8.9 Tests importants

47 unit tests + 6 integration tests. Tests pour chaque type de définition et chaque endpoint.

### 8.10 Limites actuelles

- Tout est InMemory (pas de persistance)
- Aucune authentification
- Le service est read-only

---

## 9. Player Service

### 9.1 Rôle du service

Le Player Service gère les données permanentes du joueur, indépendantes des runs : profil, personnages, roster, progression hors run et unlocks.

### 9.2 Ce que le service possède

- PlayerProfile (identité, displayName)
- PlayerCharacter (definitionKey, displayName, vitality, mana, charge, skills)
- PlayerRoster (collection de personnages)
- PlayerProgression (runs started/completed/failed)

### 9.3 Ce que le service ne doit pas posséder

- Runs, rooms, map nodes
- Combats actifs
- Rewards runtime
- Définitions Catalog

### 9.4 Structure de dossiers

```
services/player/
├── src/
│   ├── Leds.Player.Domain/         # PlayerProfile, PlayerCharacter, PlayerRoster, PlayerProgression
│   ├── Leds.Player.Application/    # CQRS, DTOs, repository interface
│   ├── Leds.Player.Infrastructure/ # InMemoryPlayerProfileRepository
│   └── Leds.Player.Api/            # Controllers, middleware
├── tests/
│   ├── Leds.Player.UnitTests/
│   └── Leds.Player.IntegrationTests/
└── Leds.Player.slnx
```

### 9.5 Domain

6 fichiers :
- `PlayerProfile` — sealed class avec PlayerId, DisplayName, Roster, Progression
- `PlayerCharacter` — sealed class avec PlayerCharacterId, DefinitionKey, DisplayName, MaxVitality, BaseMana, BaseCharge, SkillKeys
- `PlayerRoster` — sealed class wrapping List<PlayerCharacter>, déduplication par Id et DefinitionKey
- `PlayerProgression` — sealed class avec TotalRunsStarted, TotalRunsCompleted, TotalRunsFailed
- `PlayerId`, `PlayerCharacterId` — value objects (readonly record struct wrapping Guid)

### 9.6 PlayerProfile

Créé via `PlayerProfile.Create(displayName, createdAtUtc)`. Valide que le displayName n'est pas vide. Crée automatiquement un personnage par défaut "Le Porteur" avec :
- definitionKey: `character.player.self`
- displayName: "Le Porteur"
- maxVitality: 100
- skills: `skill.basic.strike`, `skill.basic.guard`

### 9.7 PlayerCharacter

Créé via `PlayerCharacter.Create(...)`. Valide :
- definitionKey non vide
- displayName non vide
- maxVitality > 0
- baseMana >= 0
- baseCharge >= 0
- au moins une skill
- aucune skill vide

### 9.8 PlayerRoster

- `AddCharacter` déduplique par Id et DefinitionKey
- `GetAvailableCharacters` retourne tous les personnages

### 9.9 PlayerProgression

- `CreateDefault()` retourne tout à zéro
- `IncrementRunsStarted/Completed/Failed` pour la progression

### 9.10 Run snapshot

Endpoint clé pour l'intégration avec Game Engine :
```http
GET /api/v2/players/{playerId}/run-snapshot
```

Retourne :
```json
{
  "playerId": "...",
  "displayName": "...",
  "characters": [
    {
      "characterId": "...",
      "definitionKey": "character.player.self",
      "displayName": "Le Porteur",
      "maxVitality": 100,
      "baseMana": 0,
      "baseCharge": 0,
      "skillKeys": ["skill.basic.strike", "skill.basic.guard"]
    }
  ]
}
```

### 9.11 Endpoints exposés

| Méthode | Route | Description |
|---|---|---|
| POST | `/api/v2/players` | Créer un profil joueur |
| GET | `/api/v2/players/{playerId}` | Récupérer le profil |
| GET | `/api/v2/players/{playerId}/run-snapshot` | Snapshot pour Game Engine |

### 9.12 Relation avec Game Engine

Game Engine appelle Player Service au démarrage d'une run pour récupérer le snapshot. Game Engine copie le snapshot dans son propre état runtime. Player Service ne connaît pas les runs ni les combats.

Les projections de résultats de run vers Player Service suivent le pattern Outbox. Game Engine écrit des événements d'intégration dans une table outbox locale dans la même transaction que son état métier, puis un dispatcher in-process les transmet à Player Service pour mettre à jour les statistiques permanentes du joueur. L'implémentation couvre `RunCompletedIntegrationEvent`, `RunFailedIntegrationEvent` et `RunAbandonedIntegrationEvent`. Voir `docs/backend/game-engine-player-outbox-projections.md` et `docs/adr/ADR-005-run-event-projections-and-player-outbox.md`.

### 9.13 Tests importants

13 unit tests + 6 integration tests :
- `PlayerProfileTests` — création, validation, personnage par défaut
- `PlayerCommandHandlerTests` — handlers CQRS
- `PlayersControllerTests` — endpoints API

### 9.14 Limites actuelles

- Tout est InMemory (pas de persistance PostgreSQL)
- Aucune authentification
- Skills hardcodées dans le mapping StartRun (displayName = key, type hardcodé)

---

## 10. Frontend web-client

### 10.1 Rôle du frontend

Le web-client est l'interface utilisateur. Il transmet des intentions au backend et affiche les résultats. Il ne calcule jamais les dégâts, ne décide pas des rewards et ne simule pas les tours ennemis.

### 10.2 Structure de dossiers

```
apps/game-client/src/
├── main.ts                          # Point d'entrée Vue
├── App.vue                          # Composant racine
├── app/
│   ├── router/index.ts              # Vue Router
│   └── layouts/GameShellLayout.vue  # Layout principal
├── features/
│   ├── combat/                      # Combat feature (actif)
│   │   ├── api/combatApi.ts
│   │   ├── components/ (6 fichiers)
│   │   ├── stores/useCombatStore.ts (409 lignes)
│   │   └── types/combatContracts.ts
│   ├── runs/                        # Run feature
│   │   ├── api/runApi.ts
│   │   ├── stores/runStore.ts (757 lignes)
│   │   ├── components/ (2 fichiers)
│   │   └── types/runTypes.ts
│   ├── rewards/                     # Reward feature
│   │   ├── api/rewardApi.ts
│   │   ├── components/RewardOfferPanel.vue
│   │   └── types/rewardTypes.ts
│   ├── events/                      # Events feature
│   │   ├── api/eventChoiceApi.ts
│   │   ├── components/ (2 fichiers)
│   │   └── types/eventTypes.ts
│   ├── interlude/                   # Interlude feature
│   ├── node-details/                # Node detail panel
│   ├── palace-laws/                 # Palace laws panel
│   ├── palace-map/                  # Map placeholder
│   ├── party/                       # Party panel
│   ├── elise/                       # Elise overlay
│   └── combats/                     # Legacy combat feature (non utilisé)
├── shared/
│   ├── api/gameEngineApi.ts         # HTTP client
│   ├── config/environment.ts
│   └── styles/tokens.css, global.css
└── pages/
    └── RunPage.vue                  # Page principale de run (406 lignes)
```

56 fichiers au total, architecture feature-sliced.

### 10.3 Routes principales

La route principale est `/run/:runId` qui charge `RunPage.vue`. La page gère tous les états : map, combat, reward, interlude, room cleared, event outcome, run suspendue, run terminée.

### 10.4 Stores principaux

| Store | Taille | Rôle |
|---|---|---|
| `runStore.ts` | 757 lignes | État run, rooms, nodes, interlude, rewards, progression |
| `useCombatStore.ts` | 409 lignes | État combat, skills, targets, logs, animations |

### 10.5 Services API frontend

| Service | Endpoints appelés |
|---|---|
| `runApi.ts` | POST/GET runs, choose-node, resolve-event, progress, abandon, save-and-exit, resume, exit-mid-room, enter-interlude, get-interlude, enter-next-room |
| `combatApi.ts` | GET current-combat, POST skill-actions |
| `rewardApi.ts` | GET pending-reward, POST select-reward |
| `eventChoiceApi.ts` | POST choose-event-option |
| `gameEngineApi.ts` | Client HTTP de base |

### 10.6 Écran Run

`RunPage.vue` (406 lignes) est la page principale. Il gère la priorité d'affichage :
1. Reward pending → RewardOfferPanel
2. Combat actif → CombatScene
3. Interlude → InterludePanel
4. Room cleared → RoomClearedPanel
5. Event outcome → EventOutcomePanel
6. Event choice result → EventChoiceResultPanel
7. Map → PalaceMapPlaceholder
8. Run suspendue → placeholder
9. Run terminée → écran de sortie

### 10.7 Écran Combat

`CombatScene.vue` affiche :
- Header avec numéro de tour
- Board avec alliés à gauche et ennemis à droite
- Footer avec SkillBar + CombatLogPanel
- Action bar avec boutons Exécuter et Annuler
- CombatantSidePanel à droite
- CombatOutcomePanel en overlay pour victoire/défaite
- Overlay de résolution pendant le traitement

`useCombatStore.ts` gère :
- `combat`, `logEntries`, `selectedSkillKey`, `selectedTargetIds`
- `isLoading`, `error`, `terminalEvent`
- Animation states : `thinkingCombatantId`, `recentlyDamagedIds`, `recentlyGuardedIds`, `recentlyDefeatedIds`, `recentlyActingId`
- `playCombatLogs` pour séquencer l'affichage des logs
- `submitAction` pour envoyer l'action au backend

### 10.8 Écran Reward

`RewardOfferPanel.vue` affiche les choix de récompense avec sélection. Le store `runStore` gère `selectReward(optionId)`.

### 10.9 Gestion des erreurs

`ExceptionHandlingMiddleware` côté backend retourne des ProblemDetails RFC 7807. Le frontend affiche les erreurs via les stores `error` refs.

### 10.10 Limites actuelles

- Legacy `combats/` feature coexiste avec le nouveau `combat/`
- Pas d'inventaire visuel
- Pas de compagnons visuels
- Pas de Rest/Law/Merchant nodes
- Pas de son ni VFX avancés

---

## 11. Persistance

### 11.1 Philosophie de persistance

Persistance relationnelle explicite. Pas de JSON. Entities EF séparées du Domain. Mapping explicite via mappers dédiés.

### 11.2 Pourquoi EF entities séparées du Domain

- Domain ne doit pas dépendre d'EF Core
- Le modèle de persistance peut différer du modèle métier
- Les invariants métier ne doivent pas être contournés par la persistance
- La réhydratation via `Rehydrate()` est explicite et contrôlée

### 11.3 PostgreSQL Game Engine

- Host: `localhost:5432`
- Database: `leds_game_engine`
- User: `postgres` / `postgres`
- Docker: `docker-compose.dev.yml`

### 11.4 Tables Game Engine

| Table | FK | Description |
|---|---|---|
| `runs` | — | Run agrégat racine |
| `run_rooms` | `run_id` → runs | Rooms d'une run |
| `run_nodes` | `room_id` → run_rooms | Nodes d'une room |
| `run_node_parent_nodes` | `map_node_id` → run_nodes | Relations parent/enfant |
| `run_active_combats` | `run_id` → runs (unique) | Combat actif |
| `run_combatants` | `combat_id` → run_active_combats | Combattants |
| `run_combatant_skills` | `combatant_id` → run_combatants | Skills des combattants |
| `run_player_states` | `run_id` → runs | État joueur runtime |
| `run_player_skills` | `run_id` → run_player_states | Skills du joueur |
| `run_memory_fragments` | `run_id` → runs | Fragments de mémoire |
| `run_active_palace_laws` | `run_id` → runs | Lois actives |

### 11.5 PostgreSQL Player

- Host: `localhost:5433`
- Database: `leds_player`
- User: `postgres` / `postgres`
- Pas encore de EF Core (InMemory seulement)

### 11.6 Ce qui est persisté

- Run complète avec rooms, nodes, progression de map
- Combat actif avec combattants et skills
- État joueur runtime (vitalité, guard, mana, charge, skills)
- Memory fragments et active palace laws
- Snapshot pour ExitMidRoom/Resume

### 11.7 Ce qui reste InMemory

- RewardOffers (InMemoryRewardOfferRepository)
- CombatInstance legacy (InMemoryCombatInstanceRepository)
- Player Service (InMemoryPlayerProfileRepository)
- Catalog Service (InMemory read stores)

### 11.8 Migrations EF Core

Commande :
```bash
dotnet ef migrations add <Name> \
  --project src/Leds.GameEngine.Infrastructure \
  --startup-project src/Leds.GameEngine.Api \
  --context GameEngineDbContext \
  --output-dir Persistence/Migrations
```

### 11.9 Stratégie future de persistance

- RewardOffers → PostgreSQL dans Game Engine
- Player Service → PostgreSQL
- Catalog Service → PostgreSQL ou rester read-only selon les besoins
- Éventuellement Event Sourcing pour l'audit des runs

---

## 12. Environnement local

### 12.1 Docker Compose

`docker-compose.dev.yml` définit deux containers PostgreSQL 16 :
- `game-engine-postgres` → port 5432, database `leds_game_engine`
- `player-postgres` → port 5433, database `leds_player`

### 12.2 Scripts de démarrage

`scripts/dev/start-dev.ps1` :
1. Vérifie Docker
2. Démarre les containers PostgreSQL
3. Ouvre une fenêtre PowerShell pour Game Engine API (`dotnet run`)
4. Ouvre une fenêtre PowerShell pour Catalog API (`dotnet run`)
5. Ouvre une fenêtre PowerShell pour Player API (`dotnet run`)
6. Ouvre une fenêtre PowerShell pour web-client (`npm run dev`)

### 12.3 Scripts d'arrêt

`scripts/dev/stop-dev.ps1` :
- Arrête les containers Docker
- Les fenêtres dotnet/npm doivent être fermées manuellement

### 12.4 Reset DB

`scripts/dev/reset-dev-db.ps1` :
- Demande confirmation
- `docker compose down -v` pour supprimer les volumes
- `docker compose up -d` pour redémarrer propres

### 12.5 Migrations

`scripts/dev/apply-migrations.ps1` :
- Applique les migrations Game Engine via `dotnet ef database update`
- Vérifie si Player a des migrations (pas encore le cas)

### 12.6 Ports locaux

| Service | Port HTTP | Port HTTPS |
|---|---|---|
| Game Engine API | 5187 | 7103 |
| Catalog API | 5193 | 7082 |
| Player API | 5189 | 7105 |
| Web Client | 5173 | — |
| Game Engine DB | 5432 | — |
| Player DB | 5433 | — |

### 12.7 URLs Swagger

- Game Engine : http://localhost:5187/swagger
- Catalog : http://localhost:5193/swagger
- Player : http://localhost:5189/swagger

### 12.8 Lancement frontend

```bash
cd apps/game-client
npm run dev
```

---

## 13. Flux de bout en bout

### 13.1 Création ou récupération d'un PlayerProfile

1. Frontend ou outil appelle `POST /api/v2/players` avec `{ displayName }`.
2. `CreatePlayerProfileCommandHandler` crée un `PlayerProfile` avec personnage par défaut.
3. Le profil est persisté via `IPlayerProfileRepository`.
4. API retourne le profil avec le roster et la progression.

### 13.2 Récupération d'un Player Run Snapshot

1. Game Engine appelle `GET /api/v2/players/{playerId}/run-snapshot`.
2. `GetPlayerRunSnapshotQueryHandler` charge le profil et extrait les personnages disponibles.
3. Retourne `PlayerRunSnapshotResponse` avec les characters, stats et skillKeys.

### 13.3 Démarrage d'une Run

1. Frontend → `POST /api/v2/runs` avec `{ playerId }`.
2. `StartRunCommandHandler` appelle `IPlayerRunSnapshotGateway.GetRunSnapshotAsync(playerId)`.
3. Le handler extrait le personnage principal et ses skills.
4. `Run.StartNew(...)` crée la run avec `PlayerRuntimeState` initialisé depuis le snapshot.
5. `IRunGenerator.GenerateInitialRoomAsync(seed)` génère la première salle.
6. La run est persistée.
7. API retourne `RunDto`.

### 13.4 Génération de la map

1. `DeterministicRunGenerator` utilise le seed pour générer le contenu.
2. `MapRoomGenerator` crée la grille de nodes (rows × lanes).
3. `MarkovRoomTypeResolver` détermine le type de room.
4. `RoomThemeResolver` détermine le thème.
5. `RoomBossProfileResolver` détermine le boss.
6. Les nodes sont connectés via `ParentNodeIds` (graphe DAG).

### 13.5 Choix d'un node

1. Frontend → `POST /nodes/{nodeId}/choose`.
2. `ChooseNodeCommandHandler` appelle `run.ChooseNode(nodeId)`.
3. Le node passe en `Selected`.
4. API retourne la run mise à jour.

### 13.6 Résolution d'un event combat

1. Frontend → `POST /current-event/resolve`.
2. `ResolveCurrentEventCommandHandler` identifie le type d'événement.
3. Pour un combat : `CombatEncounterDraftGenerator` génère le draft avec les ennemis du Catalog.
4. `CombatFactory.CreateFromDraft(draft, playerState)` crée le combat.
5. `Run.StartCombat(combat)` attache le combat.
6. API retourne la réponse avec le combat.

### 13.7 Création du combat runtime

1. `CombatFactory` crée les alliés depuis `PlayerRuntimeState` (vitalité, guard, mana, charge, skills).
2. Les ennemis sont créés depuis le draft avec les stats calculées.
3. `Combat.Create(...)` initialise le combat avec le premier allié comme combattant actif.

### 13.8 Action joueur

1. Frontend sélectionne skill + cible.
2. `POST /skill-actions` → `UseCombatSkillCommandHandler`.
3. Le handler valide l'action.
4. `CombatSkillEffectResolver` résout l'effet.
5. `PlayerRuntimeState.SyncFromCombat(...)` met à jour l'état joueur.
6. Les tours ennemis sont résolus automatiquement.
7. Le résultat est retourné au frontend.

### 13.9 Tours ennemis

1. Après l'action joueur, `ResolveEnemyTurns` est appelé.
2. Pour chaque ennemi actif, `EnemyCombatTurnResolver.Resolve(combat)` sélectionne une skill et des cibles.
3. L'effet est résolu et les logs sont ajoutés.
4. Le tour avance au combattant suivant.

### 13.10 Victoire combat

1. Le dernier ennemi est vaincu → combat `Completed`.
2. `Run.CompleteActiveCombat()` est appelé.
3. Un `RewardOffer` est créé via `RewardOfferFactory`.
4. `Run.SetPendingRewardOffer(rewardOffer.Id)` attache la reward.
5. API retourne le résultat avec `CombatCompleted = true`.

### 13.11 Création RewardOffer

1. `RewardOfferFactory.CreateCombatRewardOffer(source, eventType, riskLevel)` crée l'offre.
2. L'offre contient 3 choix Heal avec des montants différents.
3. L'offre est persistée via `IRewardOfferRepository`.

### 13.12 Sélection Reward

1. Frontend → `POST /rewards/select` avec `{ choiceId }`.
2. `SelectRewardCommandHandler` charge la run et l'offre.
3. `Run.ApplyReward(selectedChoice)` applique l'effet (heal sur PlayerRuntimeState).
4. `rewardOffer.SelectChoice(choiceId)` marque l'offre comme sélectionnée.
5. `Run.ClearPendingRewardOffer()` nettoie le pending.
6. API retourne la réponse avec la run mise à jour.

### 13.13 Reprise de Run

1. Après sélection de reward, le store frontend recharge la run.
2. `handleCombatCompleted` appelle `runApi.getRun(runId)`.
3. `progressRunInlineIfReady` avance la progression si possible.
4. La carte suivante s'affiche.

### 13.14 Défaite combat

1. Tous les alliés sont vaincus → combat `Failed`.
2. `Run.FailActiveCombat(now)` est appelé.
3. La run passe en `RunStatus.Failed`.
4. API retourne le résultat avec `CombatFailed = true`.
5. Frontend affiche l'écran de défaite avec "Quitter la run".

---

## 14. Contrats entre services

### 14.1 Game Engine → Catalog

| Endpoint | Méthode | Usage |
|---|---|---|
| `/api/v2/catalog/skills/{key}` | GET | Obtenir un template de skill |
| `/api/v2/catalog/enemies/{key}` | GET | Obtenir un template d'ennemi |
| `/api/v2/catalog/room-boss-definitions/room-type/{roomType}` | GET | Obtenir le boss d'une room |
| `/api/v2/catalog/palace-laws` | GET | Lister les lois |

Mode InMemory par défaut. Mode Http activable via `CatalogGateway:Mode`.

### 14.2 Game Engine → Player

| Endpoint | Méthode | Usage |
|---|---|---|
| `/api/v2/players/{playerId}/run-snapshot` | GET | Snapshot joueur pour démarrer une run |

Mode InMemory par défaut. Mode Http activable via `PlayerGateway:Mode`.

### 14.3 Frontend → Game Engine

Tous les endpoints API v2 du Game Engine sont appelés par le frontend via `gameEngineApi.ts`.

### 14.4 Frontend → Player

Pas d'appel direct pour l'instant. Le frontend passe par Game Engine qui appelle Player Service.

### 14.5 Contrats actuels et contrats futurs

Actuellement :
- Game Engine appelle Catalog et Player
- Frontend appelle Game Engine

Futur :
- Frontend pourrait appeler Player directement pour l'écran de profil
- Catalog pourrait exposer des endpoints de recherche avancée

---

## 15. Règles d'architecture à respecter

1. **Pas de compromis microservices** : chaque service a ses responsabilités propres.
2. **Permanent vs Runtime vs Definitions** : Player = permanent, Game Engine = runtime, Catalog = definitions.
3. **Clean Architecture** : Domain → Application → Infrastructure → API.
4. **Domain sans EF Core** : aucune référence à Entity Framework dans le Domain.
5. **Application sans Infrastructure** : les interfaces sont dans Application, les implémentations dans Infrastructure.
6. **Gateways pour les appels interservices** : abstractions dans Application, implémentations dans Infrastructure.
7. **InMemory comme fallback/test uniquement** : pas pour la production.
8. **Pas de catch silencieux** : les erreurs métier doivent être explicites.
9. **Game Engine ne rappelle pas Player pendant les actions de combat** : le snapshot est copié une fois au démarrage.
10. **Catalog ne stocke pas l'état runtime** : les définitions sont read-only.
11. **Player ne connaît pas les runs** : il fournit des snapshots, pas du runtime.
12. **Pas d'accès direct EF dans Domain/Application** : tout passe par les interfaces.
13. **Pas d'appel HTTP depuis Domain** : tout passe par les gateways.
14. **Pas de catch silencieux qui masque une erreur métier**.

---

## 16. Décisions importantes prises

| Décision | Raison |
|---|---|
| Architecture microservices stricte | Séparation claire des bounded contexts |
| Player Service créé | Données permanentes joueur dans leur propre service |
| Game Engine garde une copie runtime | Autonomie pendant la run, pas d'appel interservice à chaque action |
| EF entities séparées du Domain | Clean Architecture, pas de couplage |
| `Rehydrate()` publiques contrôlées | Restauration depuis persistance sans casser l'encapsulation |
| PostgreSQL relationnel explicite | Pas de JSON, pas d'Event Sourcing pour l'instant |
| InMemory conservé | Tests rapides, dev sans Docker |
| Docker local pour PostgreSQL | Simplicité de setup |
| MediatR CQRS | Séparation commandes/queries, pipeline de validation |
| Snapshot joueur copié au démarrage | Le Game Engine reste autonome pendant toute la durée de la run |
| Outbox pattern pour projections | Évite les dual-writes, garantit la cohérence (ADR-005) |

---

## 17. Dettes techniques connues

| Dette | Impact | Priorité |
|---|---|---|
| RewardOffers InMemory seulement | Perdues au redémarrage | Haute |
| Legacy CombatInstance coexiste | Complexité du code | Moyenne |
| Skills hardcodées dans le mapping StartRun | Skills basiques seulement | Moyenne |
| Catalog gateway partiellement HTTP | Certaines méthodes throw | Moyenne |
| `combats/` legacy frontend | Confusion potentielle | Basse |

---

## 18. Risques actuels

| Risque | Description | Mitigation |
|---|---|---|
| Complexité microservices | Plusieurs services à maintenir et déployer | Documentation, scripts de dev, architecture claire |
| Confusion permanent/runtime/definition | Données au mauvais endroit | Règles de séparation strictes, documentation |
| État du joueur duppliqué | Player profile et PlayerRuntimeState peuvent diverger | Snapshot copié une seule fois, sync après combat |
| Migrations multiples | EF migrations pour plusieurs tables | Scripts de migration automatisés |
| Documentation obsolète | Le code évolue mais la doc peut ne pas suivre | Documentation dans le repo, revue avant merge |
| Tests flaky | Les smoke tests combat dépendent du seed | Retry logic, sélection de nodes à faible risque |

---

## 19. Prochaines étapes recommandées

1. **Persister les RewardOffers dans Game Engine** — ajouter table `run_reward_offers` et `run_reward_choices`.
3. **Projections de résultats de run vers Player** — pattern Outbox implémenté. Les événements `RunCompleted`, `RunFailed`, `RunAbandoned` sont écrits dans `game_engine_outbox_messages` et dispatchés vers Player Service. L'idempotence Player est assurée via eventId. Le dispatcher est in-process pour l'instant. RabbitMQ sera envisagé dans une PR ultérieure si le volume le justifie. Voir `docs/backend/game-engine-player-outbox-projections.md`.
4. **Améliorer le mapping des skills** — utiliser le displayName et le type réel depuis le snapshot au lieu de hardcoder.
5. **Ajouter Rest Node** — node qui soigne le joueur entre les combats.
6. **Ajouter Law Node** — node qui applique des lois du palais.
7. **Ajouter NPC/Narrative Node** — node avec du dialogue et des choix narratifs.
8. **Compagnons et party runtime** — permettre au joueur d'avoir des compagnons en combat.
9. **Inventaire et économie** — items persistants, monnaie, équipement.
10. **Event Sourcing** — audit complet des actions de run (optionnel, futur lointain).

---

## 20. Index des fichiers importants

### Game Engine — Domain

| Fichier | Rôle |
|---|---|
| `src/Leds.GameEngine.Domain/Runs/Run.cs.cs` | Agrégat racine Run (927 lignes) |
| `src/Leds.GameEngine.Domain/Rooms/Room.cs` | Room avec nodes (463 lignes) |
| `src/Leds.GameEngine.Domain/Nodes/MapNode.cs` | Node spatial (227 lignes) |
| `src/Leds.GameEngine.Domain/Combats/Combat.cs` | Combat actif (227 lignes) |
| `src/Leds.GameEngine.Domain/Combats/Combatant.cs` | Combattant (214 lignes) |
| `src/Leds.GameEngine.Domain/Combats/CombatantSkill.cs` | Skill de combattant (94 lignes) |
| `src/Leds.GameEngine.Domain/Rewards/RewardOffer.cs` | Offre de récompense (85 lignes) |
| `src/Leds.GameEngine.Domain/Rewards/RewardChoice.cs` | Choix de récompense (59 lignes) |
| `src/Leds.GameEngine.Domain/Runs/PlayerRuntimeState.cs` | État joueur runtime (161 lignes) |
| `src/Leds.GameEngine.Domain/Runs/PlayerRuntimeSkill.cs` | Skill joueur runtime |
| `src/Leds.GameEngine.Domain/Common/DomainException.cs` | Exception domaine |

### Game Engine — Application

| Fichier | Rôle |
|---|---|
| `src/Leds.GameEngine.Application/Runs/StartRun/StartRunCommandHandler.cs` | Démarrage de run |
| `src/Leds.GameEngine.Application/Runs/UseCombatSkill/UseCombatSkillCommandHandler.cs` | Action combat |
| `src/Leds.GameEngine.Application/Runs/ResolveCurrentEvent/ResolveCurrentEventCommandHandler.cs` | Résolution événement |
| `src/Leds.GameEngine.Application/Rewards/SelectReward/SelectRewardCommandHandler.cs` | Sélection reward |
| `src/Leds.GameEngine.Application/Runs/Dtos/RunDto.cs` | DTO run |
| `src/Leds.GameEngine.Application/Combats/CombatFactory.cs` | Factory de combat |
| `src/Leds.GameEngine.Application/Players/Ports/IPlayerRunSnapshotGateway.cs` | Gateway Player |
| `src/Leds.GameEngine.Application/Abstractions/IRunRepository.cs` | Interface repository |

### Game Engine — Infrastructure

| Fichier | Rôle |
|---|---|
| `src/Leds.GameEngine.Infrastructure/Persistence/GameEngineDbContext.cs` | EF Core DbContext |
| `src/Leds.GameEngine.Infrastructure/Persistence/Repositories/EfRunRepository.cs` | Repository PostgreSQL |
| `src/Leds.GameEngine.Infrastructure/Persistence/Mappers/RunPersistenceMapper.cs` | Mapping Run ↔ Entity |
| `src/Leds.GameEngine.Infrastructure/Persistence/Mappers/CombatPersistenceMapper.cs` | Mapping Combat ↔ Entity |
| `src/Leds.GameEngine.Infrastructure/Persistence/Mappers/PlayerRuntimeStatePersistenceMapper.cs` | Mapping PlayerState ↔ Entity |
| `src/Leds.GameEngine.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | DI registration |
| `src/Leds.GameEngine.Infrastructure/Players/HttpPlayerRunSnapshotGateway.cs` | Gateway HTTP Player |
| `src/Leds.GameEngine.Infrastructure/Players/InMemoryPlayerRunSnapshotGateway.cs` | Gateway InMemory Player |
| `src/Leds.GameEngine.Infrastructure/Catalog/HttpCatalogContentGateway.cs` | Gateway HTTP Catalog |
| `src/Leds.GameEngine.Infrastructure/Generation/DeterministicRunGenerator.cs` | Génération de run |

### Game Engine — API

| Fichier | Rôle |
|---|---|
| `src/Leds.GameEngine.Api/Program.cs` | Composition root |
| `src/Leds.GameEngine.Api/Controllers/RunsController.cs` | Endpoints runs |
| `src/Leds.GameEngine.Api/Controllers/CombatsController.cs` | Endpoints combats |
| `src/Leds.GameEngine.Api/Controllers/RewardsController.cs` | Endpoints rewards |
| `src/Leds.GameEngine.Api/Middleware/ExceptionHandlingMiddleware.cs` | Gestion erreurs |

### Player Service

| Fichier | Rôle |
|---|---|
| `src/Leds.Player.Domain/Players/PlayerProfile.cs` | Profil joueur |
| `src/Leds.Player.Domain/Players/PlayerCharacter.cs` | Personnage joueur |
| `src/Leds.Player.Domain/Players/PlayerRoster.cs` | Roster de personnages |
| `src/Leds.Player.Application/Players/GetPlayerRunSnapshotQueryHandler.cs` | Snapshot pour Game Engine |
| `src/Leds.Player.Api/Controllers/PlayersController.cs` | Endpoints Player |

### Catalog Service

| Fichier | Rôle |
|---|---|
| `src/Leds.Catalog.Domain/Skills/ISkillTemplate.cs` | Interface skill template |
| `src/Leds.Catalog.Domain/Enemies/IEnemyTemplate.cs` | Interface enemy template |
| `src/Leds.Catalog.Application/Skills/GetSkillTemplateByKey/GetSkillTemplateByKeyQueryHandler.cs` | Handler skill |
| `src/Leds.Catalog.Infrastructure/ReadStores/InMemorySkillTemplateReadStore.cs` | Store InMemory |
| `src/Leds.Catalog.Api/Controllers/SkillTemplatesController.cs` | Endpoints skills |

### Frontend

| Fichier | Rôle |
|---|---|
| `apps/game-client/src/pages/RunPage.vue` | Page principale run (406 lignes) |
| `apps/game-client/src/features/combat/components/CombatScene.vue` | Scène combat |
| `apps/game-client/src/features/combat/stores/useCombatStore.ts` | Store combat (409 lignes) |
| `apps/game-client/src/features/runs/stores/runStore.ts` | Store run (757 lignes) |
| `apps/game-client/src/features/rewards/components/RewardOfferPanel.vue` | Panel récompense |
| `apps/game-client/src/features/combat/api/combatApi.ts` | API client combat |
| `apps/game-client/src/features/runs/api/runApi.ts` | API client run |
| `apps/game-client/src/shared/api/gameEngineApi.ts` | HTTP client de base |

### Scripts et configuration

| Fichier | Rôle |
|---|---|
| `scripts/dev/start-dev.ps1` | Démarrer l'environnement local |
| `scripts/dev/stop-dev.ps1` | Arrêter l'environnement local |
| `scripts/dev/reset-dev-db.ps1` | Réinitialiser les bases |
| `scripts/dev/apply-migrations.ps1` | Appliquer les migrations |
| `docker-compose.dev.yml` | Docker PostgreSQL local |
| `.env.example` | Variables d'environnement |
| `services/game-engine/src/Leds.GameEngine.Api/appsettings.json` | Config Game Engine |
| `services/game-engine/src/Leds.GameEngine.Api/appsettings.Development.json` | Config dev Game Engine |

---

## 21. Résumé de reprise rapide

Pour reprendre le projet demain :

1. Lire ce document, en particulier les sections 5, 7, 13 et 20.
2. Lancer l'environnement local : `.\scripts\dev\start-dev.ps1`
3. Ouvrir les Swagger :
   - Game Engine : http://localhost:5187/swagger
   - Catalog : http://localhost:5193/swagger
   - Player : http://localhost:5189/swagger
4. Créer un player : `POST /api/v2/players` avec `{ "displayName": "Thomas" }`
5. Démarrer une run : `POST /api/v2/runs` avec le playerId
6. Vérifier que le snapshot joueur est utilisé (PlayerRuntimeState avec MaxVitality 100)
7. Choisir un node et résoudre l'événement
8. Jouer un combat jusqu'à victoire
9. Vérifier que la reward s'affiche
10. Sélectionner une reward et vérifier que la run reprend

Le code est la source de vérité. Ce document est une carte pour naviguer le code efficacement.
