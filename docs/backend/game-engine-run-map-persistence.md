# Game Engine Run Map Persistence

## Objectif

Persister relationnellement l'état de map d'une Run.

## Tables

| Table | Description |
|---|---|
| `runs` | Identité, statut, progression, HP, stats, snapshot |
| `run_rooms` | Rooms appartenant à une run |
| `run_nodes` | MapNodes dans chaque room |
| `run_node_parent_nodes` | Relations parent/enfant entre nodes (M:N) |
| `run_memory_fragments` | Fragments de mémoire collectés |
| `run_active_palace_laws` | Lois du palais actives |

## Choix d'architecture

- EF Core uniquement dans Infrastructure.
- Entities persistence séparées du Domain.
- Mapping explicite Domain ↔ Persistence via `RunPersistenceMapper`.
- Méthodes `Rehydrate` publiques sur `Run`, `Room`, `MapNode`, `ActivePalaceLaw` pour la restauration.
- InMemory conservé pour tests/fallback.
- Postgres activable via `Persistence:Mode`.

## Tables relationnelles

### runs

| Colonne | Type | Description |
|---|---|---|
| id | uuid | PK |
| player_id | uuid | ID joueur |
| status | varchar(64) | RunStatus en string |
| seed | varchar(128) | Seed de génération |
| generator_version | varchar(64) | Version du générateur |
| markov_matrix_version | varchar(64) | Version de la matrice Markov |
| current_room_id | uuid | FK vers room active |
| current_room_index | int | Index 0-based de la room courante |
| max_hp / current_hp | int | Points de vie |
| attack / defense / speed | int | Stats du joueur |
| started_at_utc / ended_at_utc / saved_at_utc | timestamp | Dates clés |
| pre_suspend_status | varchar(64) | Status avant suspension |
| snapshot_* | int/nullable | Snapshot pour ExitMidRoom |

### run_rooms

| Colonne | Type | Description |
|---|---|---|
| id | uuid | PK |
| run_id | uuid | FK vers runs |
| depth / room_type / theme | | Métadonnées room |
| boss_id / boss_name / ... | | Profil du boss |
| state | varchar(64) | RoomState en string |
| current_node_depth / max_node_depth | int | Progression |
| layout_template_key / version | varchar | Template utilisé |

### run_nodes

| Colonne | Type | Description |
|---|---|---|
| id | uuid | PK |
| room_id | uuid | FK vers run_rooms |
| event_type | varchar(64) | NodeEventType en string |
| row / lane / risk_level | int | Position et risque |
| reward_profile | varchar(128) | Profil de récompense |
| is_boss | bool | Boss node |
| state | varchar(64) | NodeState en string |
| chosen_event_option_id | varchar(128) | Option choisie |

### run_node_parent_nodes

| Colonne | Type | Description |
|---|---|---|
| map_node_id | uuid | PK composite (part 1) |
| parent_node_id | uuid | PK composite (part 2) |

## Ce qui est persisté

- Identité run (ID, PlayerId, Seed, versions).
- Statut et progression.
- HP et stats joueur.
- Rooms avec boss profile.
- Nodes avec positions, états, parent/child DAG.
- Memory fragments.
- Active palace laws.
- Snapshot pour ExitMidRoom/Resume.

## Non-objectifs

- Pas de persistance ActiveCombat.
- Pas de persistance RewardOffer.
- Pas de persistance PlayerRuntimeState.
- Pas d'Event Sourcing.
- Pas de frontend.

## Prochaines étapes

- Persister ActiveCombat.
- Persister RewardOffers.
- Ajouter PlayerRuntimeState persistant.
- Brancher les nodes Rest/Law sur un état durable.

## Commandes EF Core

### Créer une migration

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Leds.GameEngine.Infrastructure \
  --startup-project src/Leds.GameEngine.Api \
  --context GameEngineDbContext \
  --output-dir Persistence/Migrations
```

### Appliquer les migrations

```bash
dotnet ef database update \
  --project src/Leds.GameEngine.Infrastructure \
  --startup-project src/Leds.GameEngine.Api \
  --context GameEngineDbContext
```
