# L’épopée des silences

> RPG roguelite narratif full web — Palais mental — Runs procédurales — Backend serveur-autoritaire.

Le projet ne suit pas de schéma de version publié pour l'instant — voir [État actuel](#état-actuel)
pour ce qui est réellement construit aujourd'hui plutôt qu'un numéro.

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

Le jeu se joue de bout en bout aujourd'hui : une run s'ouvre sur le Hall d'entrée, s'explore sur une
grille tactique libre, se peuple de PNJ qui perçoivent ou non le joueur, se heurte à des règles
locales avant de basculer en combat tactique complet. Ce qui suit décrit ce qui est réellement
construit — pas une intention.

### Exploration et génération de salle

* grille tactique à mouvement libre (`RoomGrid`) : élévation sur plusieurs paliers (montée coûteuse,
  descente gratuite), obstacles, budget de mouvement, recherche de chemin ;
* brouillard de guerre par case révélée, rayon de vision et ligne de vue — restitué côté client comme
  une mémoire d'enceinte (jamais vue / visitée / actuellement occupée) ;
* overrides de surface et placements de décor authored par cellule (tapis, piliers…), génériques à
  toute salle plutôt que réservés au Hall ;
* deux styles de génération pilotés par salle catalogue (`RoomStructuralProfile`) : `Rectangular`
  (avec sous-pièces et portes) et `Organic` (carving par rayon, jamais de sous-pièces) ;
* sorties de salle multiples posées à la génération — chaque branche accessible est visible du
  joueur, pas de tirage pondéré caché ;
* **Hall d'entrée** : première salle canon à géométrie et casting entièrement authored (grille 26×18,
  quatre piliers, escalier à sept marches, salons et alcôves, bande de tapis, seuil vers la Pièce des
  émotions) — premier consommateur concret du moteur générique ci-dessous, pas un algorithme dédié.

### PNJ positionnés et moteur d'awareness

* `RoomNpc` : PNJ physiquement présents sur la grille, avec un état de perception
  (`Unaware` / `Aware` / `Alert`) et un archétype de comportement (`Fixed` / `Guardian` / `Patrol` /
  `Hunter` / `Passive`) ;
* peints côté client par le bestiaire, famille « habitants » : Majordome, Him'Lit, Le Premier Invité,
  Échos d'Émotion (un par registre émotionnel), habitants et animaux d'ambiance.

### Règles locales et moteur de protocole

* moteur générique condition → information/avertissement → transgression → gravité → conséquences
  (`LocalRule`), data-driven et indépendant de toute salle ;
* première instance concrète : le protocole du Hall (le tapis qu'il faut essuyer, le seuil des
  émotions qu'il ne faut pas franchir).

### Dialogue et relations aux PNJ

* graphe de dialogue versionné côté Catalog, consommé par le Game Engine ;
* relations multi-axes (au-delà d'un score unique), mémoire à portée et provenance (observé / dit par
  le joueur / dit par un PNJ / rumeur / confirmé), registre de connaissance versionné, détection de
  mensonge selon personnalité et relation, conversations ambiantes déclenchées par contexte, priorité
  scénaristique > urgence > contextuelle > ambiance.

### Combat tactique

* combat sur grille tactique dédiée : déploiement, ciblage (zones d'effet, tir ami compris),
  mouvement, IA ennemie utilitaire, garde, mana, focus, effets de statut, typage émotionnel avec
  matrice d'affinité ;
* onze familles de boss canon avec comportements dédiés (Créations du Forgeron, Faux Habitants du
  Jardin, Veilleurs du Seuil, Impératrice de la Falaise, Gardiens de Crystal, Squelettes de Souvenirs,
  Échos d'Émotions, Copistes, Pénitents de la Montagne, Chimères des Plaines, Blouses Blanches).

### Catalog Service

Contenu versionné bien plus large que le socle initial : salles (+ types + affinités thématiques),
PNJ (dont leur graphe de dialogue), ennemis (+ tables de butin), boss de salle, compétences, objets
(+ types et raretés), malédictions (+ pools de récompense-malédiction), modèles de récompense + pools
de butin génériques, lois du Palais, registres émotionnels + matrice d'affinité, définitions de
combat de personnage, mondes.

### `apps/game-client`

* carte tactique isométrique peinte (`TacticalGridMap`) avec caméra, éclairage par enceinte, décor
  authored et ambiant ;
* HUD de combat : rail de portraits, garde/mana/focus, statuts, menu d'objets et de sorts ;
* panneau de dialogue PNJ ;
* panneau DevTools complet, à accès contrôlé par jeton.

### `services/player`

Squelette Clean Architecture réel (Domain/Application/Infrastructure/Api + tests), testé en CI —
au-delà du stade de dossier réservé, mais pas encore consommé par une progression joueur durable de
bout en bout.

### Ce qui n'est pas encore construit

* les zones d'influence de seuil entre salles (le Palier qui ralentit le temps, la Pièce des émotions
  qui assombrit selon la distance) — un système générique porté par la connexion entre salles, pas
  encore posé ;
* les pools d'événements du Hall (scénaristique, signature, protocole, PNJ, micro, conditionnel) et la
  rencontre signature Le Premier Invité — la donnée existe, le tirage n'est pas câblé ;
* l'interaction directe avec un `RoomNpc` (« parler à » depuis son état d'awareness) — les conséquences
  `LocalRule` qui en dépendent (changement d'attitude, fermeture d'accès, approche de Veilleurs,
  combat déclenché) restent des données correctes non encore consommées ;
* toute salle canon au-delà du Hall avec géométrie authored — les autres salles restent procédurales
  génériques ;
* `apps/player-portal` et `apps/admin-portal` — dossiers réservés dans le repository, aucun code.

---

## Architecture cible

```text
apps/
  admin-portal/    # dossier réservé, aucun code
  player-portal/   # dossier réservé, aucun code
  game-client/

services/
  game-engine/
  catalog/
  player/          # squelette Clean Architecture réel, testé en CI
  api-gateway/     # dossier réservé, aucun code
  identity/        # dossier réservé, aucun code
  audit-gdpr/      # dossier réservé, aucun code
  leaderboard/     # dossier réservé, aucun code

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
* afficher la grille tactique d’exploration (mouvement libre, brouillard de guerre, PNJ positionnés) ;
* résoudre les événements et le dialogue ;
* afficher les combats tactiques ;
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
* rooms (génération authored et procédurale, grille libre, portes/sous-pièces) ;
* PNJ positionnés et moteur d’awareness ;
* moteur générique de règles locales / protocole ;
* nodes et événements runtime ;
* dialogue et relations aux PNJ ;
* choix serveur-autoritaire ;
* combat tactique runtime ;
* récompenses runtime ;
* lois actives ;
* narration runtime ;
* orchestration de la progression.

Le Game Engine est volontairement fort : les éléments runtime fortement couplés restent dans ce bounded context.

### `services/catalog`

Service de contenu versionné.

Responsabilités :

* room templates (+ types + affinités thématiques) ;
* NPC templates (dont les graphes de dialogue) ;
* enemy templates (+ tables de butin) ;
* room boss templates ;
* skill templates ;
* item templates (+ types et raretés) ;
* curse templates (+ pools de récompense-malédiction) ;
* reward templates (+ pools de butin génériques) ;
* palace law definitions ;
* registres émotionnels + matrice d’affinité ;
* définitions de combat de personnage ;
* worlds ;
* contenus administrables.

Le Game Engine consomme Catalog via des contrats et des snapshots, sans dépendre directement de son modèle interne.

### `services/player`

Squelette Clean Architecture réel (Domain/Application/Infrastructure/Api + tests), testé en CI aux
côtés de Game Engine et Catalog. Pas encore consommé par une progression joueur durable de bout en
bout côté runtime.

### Services encore réservés

Les dossiers suivants existent dans le repository mais ne contiennent aucun code — ils seront extraits
progressivement, sans être créés prématurément tant que leur frontière n’est pas stabilisée :

```text
services/api-gateway
services/identity
services/audit-gdpr
services/leaderboard
```

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
MoveParty
EnterNode
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

Le projet ne suit pas de numérotation de version, donc pas de jalons chiffrés ici — seulement ce qui
est réellement en tête de file. Voir [État actuel](#état-actuel) pour ce qui est déjà construit.

### Prochaines priorités

```text
zones d'influence de seuil entre salles (Palier, Pièce des émotions)
pools d'événements du Hall et rencontre signature Le Premier Invité
interaction directe avec un RoomNpc ("parler à" depuis son état d'awareness)
conséquences LocalRule dépendant de cette interaction
  (changement d'attitude, fermeture d'accès, approche de Veilleurs, combat déclenché)
salles canon supplémentaires à géométrie authored, au-delà du Hall
```

Aucun de ces chantiers n’est daté — ils sont listés dans l’ordre où ils débloquent le suivant, pas
dans un ordre calendaire.

---

## Local development

```powershell
.\scripts\dev\start-dev.ps1
```

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

### Tester Player

```powershell
dotnet test services/player/Leds.Player.slnx
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

La documentation projet est dans `docs/design/` — des briefs et SFD (spécifications fonctionnelles
détaillées) par sujet plutôt qu’un document d’état unique :

```text
docs/design/sfd-combat-trpg-canonique.md          # SFD combat tactique, canonique
docs/design/brief-design-par-salle.md             # brief de design par salle
docs/design/brief-direction-artistique-combat.md
docs/design/brief-direction-artistique-ui-ux.md
docs/design/brief-murs-et-decor-exploration.md
docs/design/brief-superposition-noeuds.md         # popover de résolution au-dessus de la carte
docs/design/brief-tiroirs-et-popovers.md
docs/design/direction-visuelle-palais-respire.md
```

Les décisions techniques importantes doivent être documentées dans un fichier de suivi ou un ADR.

## CI

La branche est validée par `.github/workflows/v2-ci.yml` — service par service, sur push/PR vers
`develop` :

- Game Engine (`dotnet test`)
- Catalog (`dotnet test`)
- Player (`dotnet test`)
- frontend (`npm run build` + `npm run test`)

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
