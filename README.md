# L’épopée des silences
game-engine-alpha-0.8.10.1-InMemory-cleaning
web-alpha-0.7.6.1-fix-battle-system
catalog-alpha-0.9.0
player-service-alpha-1.0.0
data-model-alpha-1.0.0

> RPG roguelite narratif full web — Palais mental — Runs procédurales — Backend serveur-autoritaire.

**L’épopée des silences** est la refonte v2 du projet initial **RPG_ESI07**.
Le projet évolue d’une application distribuée v1 composée d’un backend monolithique, d’un portail Vue et d’un client Unity vers une architecture v2 full web, serveur-autoritaire, centrée sur un **Game Engine Service** et des services périphériques spécialisés.

La v2 est considérée comme un nouveau produit actif.
La v1 est conservée dans `legacy/` comme référence métier, technique et documentaire.

---

## Vision produit

Le joueur explore son propre **Palais mental** à travers des runs procédurales reproductibles par seed.

Chaque run représente :

* une exploration temporaire du Palais ;
* une succession de choix irréversibles ;
* une progression roguelite ;
* un chapitre écrit dans le Tome du joueur ;
* une confrontation progressive avec des souvenirs, des événements, des ennemis et des lois internes au Palais.

Le client web transmet uniquement des intentions.
Le backend décide des résultats critiques : génération, événements, combats, récompenses, progression, score et état de run.

---

## Objectifs de la v2

La v2 vise à construire progressivement :

* un jeu web principal en Vue 3 / TypeScript ;
* un Game Engine serveur-autoritaire ;
* une génération de runs déterministe et versionnée ;
* un système d’événements de carte extensible ;
* un combat runtime serveur-autoritaire ;
* une séparation claire entre progression temporaire de run et progression durable du joueur ;
* un Catalog Service pour les contenus versionnés ;
* un Player Service pour la progression durable ;
* des projections pour le Tome, le leaderboard, l’audit et les statistiques ;
* une architecture propre, testable et documentée.

---

## État actuel

Versions de travail :

* Backend Game Engine : `alpha-0.3.8`
* Frontend web-client : `alpha-0.2.1`

Fondations déjà posées :

* structure v2 du repository ;
* isolation du legacy v1 ;
* Game Engine Service en Clean Architecture ;
* Catalog Service en Clean Architecture ;
* shared-building-blocks minimal ;
* génération déterministe de runs, rooms, nodes et events ;
* contraintes de room et de node ;
* progression par chemins dans une room ;
* sélection versionnée des types de rooms et d’événements ;
* contrats Game Engine ↔ Catalog ;
* Catalog HTTP Gateway opt-in côté Game Engine ;
* définitions Catalog pour les Room Bosses ;
* définitions Catalog pour les ennemis ;
* définitions Catalog pour les skills ;
* consommation des RoomBossDefinitions, EnemyDefinitions et SkillDefinitions par le Game Engine ;
* génération de CombatEncounterDrafts ;
* composition déterministe des rencontres ;
* création d’un Combat runtime multi-alliés / multi-ennemis ;
* persistance du combat actif dans la Run ;
* endpoint de récupération du combat courant ;
* endpoint d’action de combat basé sur les skills ;
* validation des actions de combat ;
* règles de ciblage des skills ;
* résolution des effets de base : Damage, Guard, Weaken et Disrupt ;
* progression de tour déterministe ;
* tours ennemis automatiques simples ;
* fin de combat avec victoire ou défaite ;
* reprise de la progression de run après victoire ;
* passage de la run en échec après défaite ;
* documentation de transition v1 vers v2 ;
* documentation des fondations backend, du Catalog, du combat et des contrats Game Engine ↔ Catalog.

Le backend est actuellement stabilisé jusqu’à `alpha-0.3.8`. La prochaine étape backend prévue est `alpha-0.3.9`, dédiée à la stabilisation du flow complet de combat et des contrats API avant reprise du chantier frontend.

Le frontend est actuellement en `alpha-0.2.1`. Une fois le backend combat stabilisé, la prochaine phase consistera à aligner le frontend sur les nouveaux contrats de combat, puis à construire l’interface de combat jouable : scène de combat, sélection de skills, ciblage, feedbacks visuels, animations d’attaque, animations de dégâts, états de fin de combat et reprise de la run.

La prochaine cible majeure globale est `alpha-0.4.0`, qui correspondra à une première boucle de combat jouable de bout en bout côté backend et frontend.

---

## Architecture cible

```text
apps/
  admin-portal/
  player-portal/
  game-client/

services/
  game-engine/
  catalog/
  player/          # futur
  identity/        # futur
  audit-gdpr/      # futur
  leaderboard/     # futur

packages/
  shared-building-blocks/
  web-shared/      # futur

legacy/
  backend-v1/
  web-v1/
  unity/

docs/
```

---

## Applications frontend v2

La v2 prévoit trois applications web distinctes.

### `apps/game-client`

Client de jeu principal.

Responsabilités :

* démarrer ou reprendre une run ;
* afficher la carte de room ;
* choisir un node ;
* résoudre les événements ;
* afficher les combats ;
* afficher les récompenses ;
* afficher les lois actives ;
* afficher l’état temporaire de run.

Le client ne calcule pas les résultats de gameplay.
Il envoie des intentions au backend.

### `apps/player-portal`

Portail joueur hors run active.

Responsabilités :

* dashboard joueur ;
* historique des runs ;
* détail d’une run ;
* seeds jouées et rejouables ;
* ennemis rencontrés ;
* objets récupérés ;
* objets permanents ;
* statistiques ;
* Tome du joueur ;
* compagnons ;
* leaderboard.

### `apps/admin-portal`

Portail d’administration.

Responsabilités futures :

* gestion des templates Catalog ;
* ennemis ;
* objets ;
* compétences ;
* événements ;
* lois du Palais ;
* fragments narratifs ;
* saisons leaderboard ;
* modération ;
* outils internes.

---

## Services backend v2

### `services/game-engine`

Service central du runtime gameplay.

Responsabilités :

* runs ;
* rooms ;
* nodes ;
* événements runtime ;
* génération ;
* choix serveur-autoritaire ;
* combat runtime ;
* récompenses runtime ;
* lois actives ;
* narration runtime ;
* orchestration de la progression.

Le Game Engine est volontairement fort : les éléments runtime fortement couplés restent dans ce bounded context.

### `services/catalog`

Service de contenu versionné.

Responsabilités :

* enemy templates ;
* skill templates ;
* item templates ;
* event templates ;
* palace law definitions ;
* futurs NPC templates ;
* contenus administrables.

Le Game Engine consomme Catalog via des contrats et des snapshots, sans dépendre directement de son modèle interne.

### Services prévus

Les services suivants seront extraits progressivement :

```text
services/player
services/identity
services/audit-gdpr
services/leaderboard
```

Ils ne doivent pas être créés prématurément si leur frontière n’est pas encore stabilisée.

---

## Packages partagés

### `packages/shared-building-blocks`

Package partagé backend .NET minimal.

Contenu autorisé :

* `Result`;
* `Result<T>`;
* `Error`;
* primitives techniques stables ;
* abstractions transverses très génériques.

Contenu interdit :

* logique métier Game Engine ;
* types de rooms ;
* types d’événements ;
* règles de génération ;
* logique de combat ;
* logique narrative ;
* contenus spécifiques au Palais.

### `packages/web-shared`

Package frontend prévu.

Contenu futur possible :

* client HTTP de base ;
* gestion des erreurs API ;
* types publics partagés ;
* composants UI neutres ;
* helpers ;
* auth client générique.

Contenu interdit :

* logique de run ;
* logique de combat ;
* génération ;
* règles serveur ;
* calculs gameplay.

---

## Legacy

Le dossier `legacy/` contient les éléments historiques de RPG_ESI07 v1.

```text
legacy/
  backend-v1/
  web-v1/
  unity/
```

Ces éléments sont conservés comme référence :

* métier ;
* technique ;
* documentaire ;
* sécurité ;
* RGPD ;
* tests ;
* UX existante ;
* preuve de livraison v1.

Ils ne constituent plus la cible active de développement.

La v2 ne cherche pas à maintenir la compatibilité runtime avec la v1.
La migration est une migration de connaissances, pas une migration ligne à ligne.

---

## Principes d’architecture

### Backend

Le backend v2 suit les principes suivants :

* Clean Architecture ;
* CQRS / MediatR ;
* séparation Domain / Application / Infrastructure / API ;
* serveur-autoritaire ;
* tests unitaires et d’intégration ;
* dépendances orientées ports ;
* aucun couplage direct entre services ;
* versioning explicite des éléments déterministes ;
* documentation technique par jalon.

### Frontend

Le frontend v2 suit les principes suivants :

* Vue 3 ;
* TypeScript ;
* séparation par application ;
* séparation par feature ;
* composants réutilisables via `web-shared` uniquement si réellement transverses ;
* aucun calcul gameplay critique côté client ;
* appels API typés ;
* gestion claire des états de chargement et erreurs ;
* design orienté jeu pour `game-client`, dashboard pour `player-portal`, back-office pour `admin-portal`.

---

## Règle serveur-autoritaire

Le frontend envoie des intentions :

```text
StartRun
SelectNode
ResolveEventChoice
ChooseCombatAction
SelectReward
AbandonRun
```

Le backend décide :

```text
génération
événements
résultats de combat
récompenses
progression
inventaire
score
leaderboard
état de run
```

Aucune logique critique ne doit être calculée uniquement côté client.

---

## Roadmap synthétique

### `alpha-0.0.x`

Fondations techniques.

```text
alpha-0.0.1 → structure v2
alpha-0.0.2 → Game Engine skeleton
alpha-0.0.3 → Run / Room / Node foundations
alpha-0.0.4 → Catalog + shared-building-blocks
alpha-0.0.5 → génération déterministe versionnée
alpha-0.0.6 → pipeline typé de résolution événementielle
```

### Prochaines étapes backend

```text
alpha-0.0.7  → ResolveNodeEventCommand
alpha-0.0.8  → Combat runtime domain
alpha-0.0.9  → Start combat from resolved event
alpha-0.0.10 → Combat action flow
alpha-0.0.11 → Reward offer flow
alpha-0.0.12 → Minimal room loop completion
```

### `alpha-0.1.0`

Première boucle backend jouable.

Critère :

```text
Une room complète peut être parcourue côté backend,
avec choix de node, résolution d’événement,
combat minimal, récompense minimale et boss de room.
```

### Prochaines étapes frontend

```text
web-alpha-0.0.1 → initialiser apps/game-client
web-alpha-0.0.2 → initialiser apps/player-portal
web-alpha-0.0.3 → initialiser apps/admin-portal
web-alpha-0.0.4 → ajouter run playground
web-alpha-0.1.0 → afficher la première boucle backend jouable
```

---

## Local development

```powershell
.\scripts\dev\start-dev.ps1
```

See [docs/development/local-dev-environment.md](docs/development/local-dev-environment.md).

### Bases de données locales

`docker-compose.dev.yml` (et `docker-compose.yml`) provisionnent les trois bases Postgres :

| Service      | Conteneur                   | Port hôte | Base              |
|--------------|-----------------------------|-----------|-------------------|
| Game Engine  | `leds-game-engine-postgres` | `5432`    | `leds_game_engine`|
| Player       | `leds-player-postgres`      | `5433`    | `leds_player`     |
| Catalog      | `leds-catalog-postgres`     | `5434`    | `leds_catalog`    |

```powershell
docker compose -f docker-compose.dev.yml up -d
.\scripts\dev\apply-migrations.ps1
```

Copier `.env.example` (racine) et `apps/game-client/.env.example` fournit des valeurs
cohérentes (le client web vise le Game Engine sur `http://localhost:5187`). Les services
backend tournent en `Persistence:Mode=InMemory` par défaut ; passer une base en Postgres se
fait via la chaîne de connexion correspondante (ex. `CATALOG_DB_CONNECTION_STRING`).

## Architecture

See [docs/architecture/alpha-0.5-system-architecture.md](docs/architecture/alpha-0.5-system-architecture.md).

## Gameplay Data Model

See [docs/data-model/00-data-model-0.1-overview.md](docs/data-model/00-data-model-0.1-overview.md).

---

## Commandes utiles

### Tester le Game Engine

```powershell
dotnet test services/game-engine/Leds.GameEngine.slnx
```

### Tester Catalog

```powershell
dotnet test services/catalog/Leds.Catalog.slnx
```

### Tester les shared-building-blocks

```powershell
dotnet test packages/shared-building-blocks/Leds.SharedBuildingBlocks.slnx
```

### Formater le Game Engine

```powershell
dotnet format services/game-engine/Leds.GameEngine.slnx
```

### Vérifier les dépendances interdites

```powershell
Get-ChildItem services/game-engine/src/Leds.GameEngine.Domain -Recurse -Filter *.csproj |
  Select-String "Application|Infrastructure|Api|Catalog"
```

```powershell
Get-ChildItem services/game-engine/src/Leds.GameEngine.Application -Recurse -Filter *.csproj |
  Select-String "Infrastructure"
```

```powershell
Get-ChildItem services/game-engine -Recurse -Filter *.csproj |
  Select-String "Leds.Catalog"
```

Résultat attendu : aucune dépendance interdite.

---

## Documentation

La documentation projet est dans :

```text
docs/
```

Documents importants :

```text
docs/v2/
docs/v2/follow-up/
docs/v2/roadmap/
docs/v2/architecture/
```

Les décisions techniques importantes doivent être documentées dans un fichier de suivi ou un ADR.

## CI

The v2 branch is validated by `.github/workflows/v2-ci.yml`.

Backend services are tested automatically on push/PR to `develop`:

- Game Engine
- Catalog
- Player
- web-client

See [docs/development/ci.md](docs/development/ci.md).

---

## Règles de contribution internes

Chaque PR doit idéalement contenir :

* un objectif clair ;
* un périmètre limité ;
* du code testé ;
* des tests unitaires ou d’intégration ;
* une documentation de suivi si la décision est structurante ;
* un commit conventionnel.

Exemples de commits :

```text
feat(game-engine): expose node event resolution use case
feat(game-engine): introduce combat runtime domain
feat(catalog): add event template definitions
docs(v2): add versioning roadmap
chore(repo): move v1 backend and web portal to legacy
```

---

## Licence et propriété intellectuelle

Le code source a vocation à être open source selon la licence définie dans le dépôt.

Cependant, l’univers narratif de **L’épopée des silences** reste protégé :

* nom du projet ;
* Tome des silences ;
* personnages ;
* textes ;
* fragments narratifs ;
* lore ;
* visuels ;
* logos ;
* assets ;
* concepts d’univers ;
* contenu littéraire associé.

La licence du code ne vaut pas abandon des droits d’auteur sur l’univers, les textes, les noms, les personnages ou les assets narratifs.

---

## Statut

Projet en développement actif.

## Licence

Le code source de ce dépôt est distribué sous licence GNU Affero General Public License v3.0.

SPDX-License-Identifier: AGPL-3.0-only

Cette licence concerne le code source uniquement.  
L’univers narratif, les textes, personnages, noms, fragments, assets, logos et éléments du Tome des silences restent protégés par droit d’auteur et ne sont pas placés sous AGPL.
