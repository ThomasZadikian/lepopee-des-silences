# L’épopée des silences — Suivi technique alpha-0.0.4

## PR — Catalog read flows : Palace Laws, Event Templates, CQRS, Infrastructure et API

**Branche cible :** `v2/develop`  
**Service concerné :** `services/catalog`  
**Version concernée :** `alpha-0.0.4`  
**Type de PR :** feature Catalog / Clean Architecture / CQRS / API read flows  
**Commit recommandé :** `feat(catalog): add palace law and event template read flows`  
**Statut final :** tous les tests passent au moment du suivi

---

## 1. Contexte de la PR

Cette PR s’inscrit dans la version `alpha-0.0.4`, consacrée à la fondation du **Catalog Service**.

Le Catalog Service est le service responsable des contenus versionnés du jeu :

```text
EnemyTemplate
SkillTemplate
ItemTemplate
PalaceLawDefinition
EventTemplate
NpcTemplate
RewardTemplate
NarrativeFragmentTemplate
```

Les PR précédentes avaient posé :

```text
- le squelette Clean Architecture du service Catalog ;
- les primitives de contenu versionné ;
- les premiers templates gameplay Enemy / Skill / Item ;
- l’introduction de MediatR, CQRS, FluentValidation, DTOs, ports applicatifs et read stores InMemory.
```

Cette PR finalise un premier vrai flux de lecture Catalog complet pour deux domaines centraux de la v2 :

```text
PalaceLawDefinition
EventTemplate
```

---

## 2. Objectif de la PR

L’objectif était de ne pas se limiter à ajouter des classes de domaine.

La PR devait intégrer les Lois du Palais et les EventTemplates dans toute la chaîne Clean Architecture :

```text
Domain
Application
Infrastructure
API
Tests
```

Objectif fonctionnel :

```text
Permettre au Catalog Service d’exposer les définitions actives de Lois du Palais
et les templates d’événements via des routes HTTP propres, validées, testées,
et découplées de l’Infrastructure.
```

Objectif architectural :

```text
Respecter CQRS/MediatR, FluentValidation, l’inversion de dépendance,
les ports applicatifs, les controllers API, et la séparation stricte des couches.
```

---

## 3. Chaîne Clean Architecture validée

La PR valide désormais la chaîne complète suivante :

```text
Controller
→ reçoit la requête HTTP

ISender / MediatR
→ transmet une Query

ValidationBehavior
→ exécute les validators FluentValidation

Handler
→ exécute le cas d’usage applicatif

Port Application
→ décrit le besoin de lecture

ReadStore Infrastructure
→ fournit l’implémentation concrète temporaire

DTO Application
→ structure la réponse

Controller
→ retourne la réponse HTTP
```

La direction de dépendance reste conforme :

```text
API → Application → Domain

Infrastructure → Application → Domain

Domain → aucune couche
Application → pas Infrastructure
API → pas Infrastructure directement
```

---

## 4. Domain — PalaceLawDefinition

### 4.1 Fichiers concernés

```text
src/Leds.Catalog.Domain/PalaceLaws/IPalaceLawDefinition.cs
src/Leds.Catalog.Domain/PalaceLaws/PalaceLawDefinition.cs
src/Leds.Catalog.Domain/PalaceLaws/PalaceLawImpactDomain.cs
src/Leds.Catalog.Domain/PalaceLaws/PalaceLawVisibility.cs
```

### 4.2 Rôle métier

`PalaceLawDefinition` représente une **définition cataloguée** d’une Loi du Palais.

Elle ne représente pas une Loi active dans une run.

Séparation cible :

```text
PalaceLawDefinition
→ Catalog
→ définition versionnée, activable/désactivable

ActivePalaceLaw
→ Game Engine
→ snapshot runtime appliqué à une run
```

### 4.3 Domaines d’impact

Les Lois peuvent cibler plusieurs domaines :

```text
Generation
Events
Combat
Rewards
Narrative
HimLit
```

Cette structure prépare les futurs effets de Lois sur :

```text
- la génération de rooms/nodes ;
- les événements ;
- le combat ;
- les récompenses ;
- la narration ;
- l’adaptation finale d’Him’Lit.
```

### 4.4 Règles métier

Les règles introduites ou validées sont :

```text
Priority non négative
Au moins un ImpactDomain obligatoire
Suppression des doublons dans ImpactDomains
Cycle de vie hérité de CatalogContentBase
```

---

## 5. Domain — EventTemplate

### 5.1 Fichiers concernés

```text
src/Leds.Catalog.Domain/EventTemplates/IEventTemplate.cs
src/Leds.Catalog.Domain/EventTemplates/EventTemplate.cs
src/Leds.Catalog.Domain/EventTemplates/EventTemplateType.cs
src/Leds.Catalog.Domain/EventTemplates/EventOutcomeKind.cs
```

### 5.2 Rôle métier

`EventTemplate` représente une **définition possible d’événement**.

Il ne représente pas un événement déjà résolu pendant une run.

Séparation cible :

```text
EventTemplate
→ Catalog
→ définition versionnée d’un événement possible

NodeEventOutcome / ResolvedEvent
→ Game Engine
→ résultat runtime d’un événement résolu pendant une run
```

### 5.3 Types d’événements

Les types introduits couvrent les grandes familles prévues :

```text
Combat
Elite
Item
Npc
Memory
Rest
Merchant
Law
Curse
Rare
RoomBoss
```

### 5.4 Outcomes par défaut

Les outcomes par défaut préparent les résolutions futures :

```text
None
CombatStarted
EliteEncounterStarted
RewardGranted
NarrativeFragmentRevealed
TomePageUnlocked
RestResolved
TradeResolved
PalaceLawApplied
CurseAccepted
RareEventResolved
RoomBossStarted
```

### 5.5 Règles métier

Les règles validées sont :

```text
MinRiskLevel entre 0 et 100
MaxRiskLevel entre 0 et 100
MinRiskLevel <= MaxRiskLevel
NarrativeTags optionnels
NarrativeTags trimés
NarrativeTags dédupliqués
NarrativeTags vides ignorés
Cycle de vie hérité de CatalogContentBase
```

---

## 6. Application — CQRS pour Palace Laws

### 6.1 DTO

```text
Application/PalaceLaws/Dtos/PalaceLawDefinitionDto.cs
```

Champs exposés :

```text
Id
Key
Name
Description
Version
Status
Visibility
Priority
ImpactDomains
```

### 6.2 Port applicatif

```text
Application/PalaceLaws/Ports/IPalaceLawDefinitionReadStore.cs
```

Le port définit le besoin applicatif :

```text
ListActiveAsync
GetByKeyAsync
```

L’Application ne connaît pas le stockage réel.

### 6.3 Query GetByKey

```text
Application/PalaceLaws/GetPalaceLawDefinitionByKey/
→ GetPalaceLawDefinitionByKeyQuery.cs
→ GetPalaceLawDefinitionByKeyResponse.cs
→ GetPalaceLawDefinitionByKeyQueryValidator.cs
→ GetPalaceLawDefinitionByKeyQueryHandler.cs
```

Validation :

```text
Key obligatoire
Key maximum 128 caractères
```

### 6.4 Query ListActive

```text
Application/PalaceLaws/ListActivePalaceLawDefinitions/
→ ListActivePalaceLawDefinitionsQuery.cs
→ ListActivePalaceLawDefinitionsResponse.cs
→ ListActivePalaceLawDefinitionsQueryHandler.cs
```

Pas de validator nécessaire car la query ne porte aucune entrée externe.

---

## 7. Application — CQRS pour Event Templates

### 7.1 DTO

```text
Application/EventTemplates/Dtos/EventTemplateDto.cs
```

Champs exposés :

```text
Id
Key
Name
Description
Version
Status
Type
DefaultOutcomeKind
MinRiskLevel
MaxRiskLevel
RequiresPlayerChoice
NarrativeTags
```

### 7.2 Port applicatif

```text
Application/EventTemplates/Ports/IEventTemplateReadStore.cs
```

Le port définit :

```text
ListActiveAsync
GetByKeyAsync
```

### 7.3 Query GetByKey

```text
Application/EventTemplates/GetEventTemplateByKey/
→ GetEventTemplateByKeyQuery.cs
→ GetEventTemplateByKeyResponse.cs
→ GetEventTemplateByKeyQueryValidator.cs
→ GetEventTemplateByKeyQueryHandler.cs
```

Validation :

```text
Key obligatoire
Key maximum 128 caractères
```

### 7.4 Query ListActive

```text
Application/EventTemplates/ListActiveEventTemplates/
→ ListActiveEventTemplatesQuery.cs
→ ListActiveEventTemplatesResponse.cs
→ ListActiveEventTemplatesQueryHandler.cs
```

---

## 8. Infrastructure — ReadStores InMemory

### 8.1 Objectif

Les read stores InMemory fournissent une implémentation temporaire des ports Application.

Ils permettent :

```text
- de tester les flux complets ;
- d’exposer les premières routes API ;
- d’éviter d’introduire EF Core trop tôt ;
- de remplacer plus tard InMemory par PostgreSQL sans modifier les handlers.
```

### 8.2 ReadStore PalaceLaw

```text
Infrastructure/ReadStores/InMemoryPalaceLawDefinitionReadStore.cs
```

Contenus seedés :

```text
law-silence-v1
law-rupture-v1
```

### 8.3 ReadStore EventTemplate

```text
Infrastructure/ReadStores/InMemoryEventTemplateReadStore.cs
```

Contenus seedés :

```text
event-memory-threshold-v1
event-law-silence-v1
event-combat-shadow-v1
```

### 8.4 ReadStores Enemy / Skill / Item

La PR a aussi consolidé les tests autour des read stores déjà présents :

```text
InMemoryEnemyTemplateReadStore
InMemorySkillTemplateReadStore
InMemoryItemTemplateReadStore
```

### 8.5 Dependency Injection

```text
Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs
```

Le conteneur DI relie désormais :

```text
IEnemyTemplateReadStore
→ InMemoryEnemyTemplateReadStore

ISkillTemplateReadStore
→ InMemorySkillTemplateReadStore

IItemTemplateReadStore
→ InMemoryItemTemplateReadStore

IPalaceLawDefinitionReadStore
→ InMemoryPalaceLawDefinitionReadStore

IEventTemplateReadStore
→ InMemoryEventTemplateReadStore
```

Cette étape matérialise l’inversion de dépendance.

---

## 9. API — Controllers et Middleware

### 9.1 Choix architectural

La PR utilise des controllers ASP.NET Core, et non des routes Minimal API directement dans `Program.cs`.

Décision :

```text
Conserver l’organisation API déjà présente dans game-engine :
Controllers
Middleware
Program.cs minimal
```

Raisons :

```text
- cohérence avec le service game-engine ;
- cohérence avec le backend legacy ;
- meilleure lisibilité ;
- séparation explicite des responsabilités ;
- architecture plus familière pour le projet.
```

### 9.2 Program.cs

`Program.cs` reste un composition root :

```text
- AddCatalogApplication
- AddCatalogInfrastructure
- AddControllers
- Swagger
- Middleware
- MapControllers
```

Il ne contient pas les routes métier.

### 9.3 Middleware

```text
Api/Middleware/CatalogExceptionHandlingMiddleware.cs
Api/Middleware/CatalogExceptionHandlingExtensions.cs
```

Rôle :

```text
Capturer les ValidationException FluentValidation
et retourner un 400 BadRequest au format application/problem+json.
```

### 9.4 Controllers ajoutés ou consolidés

```text
Api/Controllers/CatalogHealthController.cs
Api/Controllers/EnemyTemplatesController.cs
Api/Controllers/SkillTemplatesController.cs
Api/Controllers/ItemTemplatesController.cs
Api/Controllers/PalaceLawDefinitionsController.cs
Api/Controllers/EventTemplatesController.cs
```

Chaque controller suit la règle :

```text
Recevoir HTTP
Créer une Query
Appeler ISender.Send(...)
Retourner Ok / NotFound
Ne pas faire de logique métier
Ne pas dépendre de l’Infrastructure
```

---

## 10. Routes API exposées

### Health

```text
GET /api/v2/catalog/health
```

### Enemy Templates

```text
GET /api/v2/catalog/enemies
GET /api/v2/catalog/enemies/{key}
```

### Skill Templates

```text
GET /api/v2/catalog/skills
GET /api/v2/catalog/skills/{key}
```

### Item Templates

```text
GET /api/v2/catalog/items
GET /api/v2/catalog/items/{key}
```

### Palace Law Definitions

```text
GET /api/v2/catalog/palace-laws
GET /api/v2/catalog/palace-laws/{key}
```

### Event Templates

```text
GET /api/v2/catalog/event-templates
GET /api/v2/catalog/event-templates/{key}
```

---

## 11. Point technique important : validation des routes

Une erreur a été rencontrée sur les tests `BadRequest_WhenKeyIsWhitespace`.

### Symptôme

ASP.NET Core retournait son propre 400 :

```text
One or more validation errors occurred.
The key field is required.
```

au lieu de passer par FluentValidation.

### Cause

Avec `[ApiController]` et un paramètre non-nullable :

```csharp
string key
```

ASP.NET Core peut intercepter la requête avant MediatR.

### Correction

Les paramètres route `key` ont été rendus nullable :

```csharp
string? key
```

Puis les queries reçoivent :

```csharp
key ?? string.Empty
```

Cela garantit le flux souhaité :

```text
Controller
→ ISender.Send(query)
→ ValidationBehavior
→ FluentValidation
→ 400 applicatif
```

---

## 12. Tests unitaires

### 12.1 Domain

Les tests couvrent :

```text
CatalogContent
EnemyTemplate
SkillTemplate
ItemTemplate
PalaceLawDefinition
EventTemplate
```

### 12.2 Application

Les tests couvrent :

```text
ValidationBehavior
Validators
Handlers
DTO mappings
Ports mockés
Cas trouvé / non trouvé
ListActive
```

### 12.3 Infrastructure

Les tests couvrent les read stores :

```text
InMemoryEnemyTemplateReadStoreTests
InMemorySkillTemplateReadStoreTests
InMemoryItemTemplateReadStoreTests
InMemoryPalaceLawDefinitionReadStoreTests
InMemoryEventTemplateReadStoreTests
```

Points vérifiés :

```text
- ListActiveAsync retourne uniquement des contenus actifs ;
- GetByKeyAsync retourne le contenu existant ;
- GetByKeyAsync est insensible à la casse ;
- GetByKeyAsync retourne null si la clé n’existe pas ;
- les contenus seedés attendus sont présents.
```

---

## 13. Tests d’intégration

Les tests d’intégration vérifient la vraie API :

```text
DI complète
Controllers
MediatR
ValidationBehavior
Middleware
ReadStores InMemory
JSON de réponse
Status codes HTTP
```

Dossiers concernés :

```text
tests/Leds.Catalog.IntegrationTests/Health
tests/Leds.Catalog.IntegrationTests/Enemies
tests/Leds.Catalog.IntegrationTests/Skills
tests/Leds.Catalog.IntegrationTests/Items
tests/Leds.Catalog.IntegrationTests/PalaceLaws
tests/Leds.Catalog.IntegrationTests/EventTemplates
```

Scénarios testés :

```text
200 OK sur les listes
200 OK sur GetByKey existant
404 NotFound sur GetByKey inexistant
400 BadRequest sur key invalide
payload DTO conforme
```

---

## 14. Ce que cette PR valide

Cette PR valide un read flow complet du Catalog :

```text
HTTP
→ Controller
→ Query
→ Validator
→ Handler
→ Port
→ ReadStore
→ Domain
→ DTO
→ Response
```

Ce flux est maintenant disponible pour :

```text
EnemyTemplate
SkillTemplate
ItemTemplate
PalaceLawDefinition
EventTemplate
```

---

## 15. Pourquoi cette PR est importante

Cette PR est structurante pour la suite de la v2.

Elle permet d’éviter :

```text
- un Game Engine qui porterait directement les référentiels de contenu ;
- une API Catalog qui appellerait l’Infrastructure directement ;
- des routes HTTP non organisées dans Program.cs ;
- des validators ajoutés trop tard ;
- une future migration EF Core douloureuse ;
- un couplage entre modèles Domain et réponses HTTP.
```

Elle prépare directement :

```text
- le branchement futur Game Engine ↔ Catalog ;
- le Combat MVP ;
- la résolution d’événements depuis des templates ;
- l’application des Lois du Palais depuis le Catalog ;
- la migration progressive des données legacy v1.
```

---

## 16. État final de la PR

À la fin de la PR :

```text
- les tests unitaires passent ;
- les tests d’intégration passent ;
- le build passe ;
- Program.cs reste minimal ;
- les controllers sont alignés avec le standard du game-engine ;
- les read stores InMemory sont couverts ;
- les validators sont branchés via MediatR pipeline.
```

---

## 17. Commandes de validation

Depuis `services/catalog` :

```bash
dotnet format Leds.Catalog.slnx
dotnet test Leds.Catalog.slnx
```

Puis vérifier le Game Engine :

```bash
cd ../game-engine
dotnet test Leds.GameEngine.slnx
```

---

## 18. Commit recommandé

Depuis la racine du repo :

```bash
git add services/catalog docs/v2/follow-up/SUIVI_TECHNIQUE_ALPHA_0_0_4_CATALOG_READ_FLOWS_FINAL.md

git commit -m "feat(catalog): add palace law and event template read flows"

git push
```

---

## 19. Position dans alpha-0.0.4

Position recommandée :

```text
alpha-0.0.4

PR 1 — Catalog domain primitives + gameplay templates
PR 2 — Application CQRS validation foundation
PR 3 — Palace law and event template read flows
```

Cette PR est la première à exposer un flux Catalog complet sur plusieurs familles de contenu.

---

## 20. Suite recommandée

Après cette PR, la suite logique sera :

```text
feat(catalog): add catalog content filtering and metadata
```

ou :

```text
feat(game-engine): prepare catalog read contracts integration
```

Avant de brancher le Game Engine, il faudra clarifier :

```text
- le contrat exact attendu par le Game Engine ;
- les snapshots runtime nécessaires ;
- les règles de versioning de contenu ;
- la manière dont une run référence une version Catalog.
```
