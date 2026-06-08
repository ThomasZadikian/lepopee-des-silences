# L’épopée des silences - aplha-0.1.8 / web-alpha-0.1.4

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

Version de travail : `alpha-0.0.6`

Fondations déjà posées :

* structure v2 du repository ;
* isolation du legacy v1 ;
* Game Engine Service en Clean Architecture ;
* Catalog Service amorcé ;
* shared-building-blocks minimal ;
* génération de runs, rooms et nodes ;
* contraintes de room et de node ;
* génération déterministe par seed ;
* sélection versionnée des types de rooms et d’événements ;
* contrats Game Engine ↔ Catalog ;
* pipeline typé de résolution de contenu événementiel ;
* stratégie de transition v1 vers v2 documentée.

La prochaine cible majeure est `alpha-0.1.0`, qui correspondra à une première boucle backend jouable de bout en bout.

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
