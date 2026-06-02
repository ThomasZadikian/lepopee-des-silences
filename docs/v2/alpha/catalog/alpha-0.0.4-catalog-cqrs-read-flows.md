# L’épopée des silences — Suivi technique alpha-0.0.4

## PR en cours — Catalog Application CQRS, Validators et Read Flows PalaceLaw/EventTemplate

**Branche cible :** `v2/develop`  
**Service concerné :** `services/catalog`  
**Version concernée :** `alpha-0.0.4`  
**Statut des tests :** 94 tests passants au moment du suivi  
**Commit final visé :** `feat(catalog): add palace law and event template read flows`

---

## 1. Contexte

Cette étape intervient après la création du `Catalog Service` et après la mise en place des premières fondations de domaine :

```text
CatalogContent
EnemyTemplate
SkillTemplate
ItemTemplate
PalaceLawDefinition
EventTemplate
```

L’objectif de cette session n’était pas seulement d’ajouter des classes de domaine, mais de les intégrer correctement dans l’architecture cible :

```text
Domain
Application
Infrastructure
Api
Tests
```

Point corrigé pendant cette étape :

```text
Un modèle Domain seul ne suffit pas.
Un service Clean Architecture doit aussi exposer ses cas d’usage via Application/CQRS,
ses dépendances via des ports,
ses implémentations via Infrastructure,
et ses entrées HTTP via API/MediatR.
```

---

## 2. Pourquoi cette étape était nécessaire

Une première version du Catalog ajoutait surtout des modèles de domaine.

Cela permettait de représenter le contenu, mais pas encore de répondre proprement aux questions suivantes :

```text
Comment l’API lit-elle un template ?
Comment valide-t-on une requête ?
Comment évite-t-on que l’API dépende directement de l’Infrastructure ?
Comment remplace-t-on plus tard le InMemory par EF Core sans casser l’Application ?
Comment le Game Engine consommera-t-il le Catalog sans connaître ses détails techniques ?
```

La réponse apportée par cette PR est :

```text
CQRS + MediatR + FluentValidation + Ports applicatifs + ReadStores Infrastructure
```

---

## 3. Architecture cible respectée

La dépendance correcte est désormais :

```text
Api
→ Application
→ Domain

Infrastructure
→ Application
→ Domain
```

Et jamais :

```text
Domain → Application
Application → Infrastructure
Api → Infrastructure directement pour les cas d’usage
```

Le `Domain` reste pur :

```text
- invariants métier
- value objects
- templates
- interfaces de domaine
- exceptions métier
```

L’`Application` orchestre :

```text
- queries
- handlers
- validators
- DTOs
- ports
```

L’`Infrastructure` implémente :

```text
- read stores temporaires InMemory
- futurs repositories EF Core
- futurs adapters techniques
```

L’`Api` expose :

```text
- endpoints HTTP
- appels à ISender.Send(...)
- gestion des erreurs de validation
```

---

## 4. Ce que représente CQRS dans cette étape

CQRS signifie que l’on sépare les intentions de lecture et d’écriture.

Dans cette PR, on travaille uniquement sur des lectures :

```text
GetByKey
ListActive
```

Exemples :

```text
GetPalaceLawDefinitionByKeyQuery
ListActivePalaceLawDefinitionsQuery
GetEventTemplateByKeyQuery
ListActiveEventTemplatesQuery
```

Chaque query représente une intention métier claire :

```text
"Je veux récupérer une Loi du Palais par sa clé."
"Je veux lister les Lois actives."
"Je veux récupérer un EventTemplate par sa clé."
"Je veux lister les EventTemplates actifs."
```

Les handlers exécutent ces intentions.

---

## 5. Pourquoi MediatR est utilisé

MediatR sert à éviter que l’API appelle directement les services, repositories ou read stores.

Au lieu de faire :

```text
Endpoint
→ ReadStore directement
```

on fait :

```text
Endpoint
→ ISender.Send(query)
→ Handler
→ Port applicatif
→ Infrastructure
```

Flux type :

```text
GET /api/v2/catalog/palace-laws/law-silence-v1
→ GetPalaceLawDefinitionByKeyQuery
→ ValidationBehavior
→ GetPalaceLawDefinitionByKeyQueryHandler
→ IPalaceLawDefinitionReadStore
→ InMemoryPalaceLawDefinitionReadStore
→ PalaceLawDefinitionDto
→ Response HTTP
```

---

## 6. Pourquoi FluentValidation est utilisé

Les validators empêchent les entrées invalides d’atteindre les handlers.

Exemple :

```text
GetEventTemplateByKeyQuery
→ Key obligatoire
→ Key maximum 128 caractères
```

Règle posée :

```text
Toute Query ou Command recevant une entrée externe doit avoir un Validator.
```

Dans cette étape, les validators ajoutés concernent :

```text
GetPalaceLawDefinitionByKeyQueryValidator
GetEventTemplateByKeyQueryValidator
```

Les queries `ListActive...` n’ont pas de validator car elles ne portent aucun paramètre externe.

---

## 7. Rôle du ValidationBehavior

Le `ValidationBehavior<TRequest, TResponse>` est un pipeline MediatR.

Il intercepte une requête avant son handler :

```text
Query envoyée
→ ValidationBehavior
→ Validators associés
→ Handler si valide
→ ValidationException si invalide
```

Avantages :

```text
Les validators sont exécutés automatiquement.
Les handlers restent concentrés sur le cas d’usage.
La validation est centralisée.
```

---

## 8. Pourquoi des DTOs Application

Le Domain ne doit pas être retourné directement par l’API.

On crée donc des DTOs :

```text
PalaceLawDefinitionDto
EventTemplateDto
```

Ils transforment les modèles Domain en objets de sortie applicatifs.

Cela évite :

```text
- d’exposer directement le modèle métier ;
- de coupler l’API aux entités Domain ;
- de rendre les futures évolutions Domain cassantes pour les clients.
```

---

## 9. Pourquoi des ports applicatifs

Un port applicatif est une interface déclarée dans `Application`.

Exemples :

```text
IPalaceLawDefinitionReadStore
IEventTemplateReadStore
```

Ces ports expriment ce dont l’Application a besoin :

```text
ListActiveAsync
GetByKeyAsync
```

Ils ne disent pas comment les données sont stockées.

Aujourd’hui, l’implémentation sera InMemory.

Demain, elle pourra être EF Core, PostgreSQL, cache, fichier seedé ou service externe.

Le handler ne changera pas.

---

## 10. Différence entre interface Domain et port Application

### Interfaces Domain

Exemples :

```text
ICatalogContent
IEnemyTemplate
ISkillTemplate
IItemTemplate
IPalaceLawDefinition
IEventTemplate
```

Elles décrivent ce qu’est un objet métier.

### Ports Application

Exemples :

```text
IPalaceLawDefinitionReadStore
IEventTemplateReadStore
```

Ils décrivent ce dont un cas d’usage a besoin pour fonctionner.

Résumé :

```text
Interface Domain
→ décrit le modèle métier

Port Application
→ décrit un besoin applicatif
```

---

## 11. PalaceLawDefinition — intégration Application

### Fichiers ajoutés côté Application

```text
Application/PalaceLaws/Dtos/PalaceLawDefinitionDto.cs

Application/PalaceLaws/Ports/IPalaceLawDefinitionReadStore.cs

Application/PalaceLaws/GetPalaceLawDefinitionByKey/
→ GetPalaceLawDefinitionByKeyQuery.cs
→ GetPalaceLawDefinitionByKeyResponse.cs
→ GetPalaceLawDefinitionByKeyQueryValidator.cs
→ GetPalaceLawDefinitionByKeyQueryHandler.cs

Application/PalaceLaws/ListActivePalaceLawDefinitions/
→ ListActivePalaceLawDefinitionsQuery.cs
→ ListActivePalaceLawDefinitionsResponse.cs
→ ListActivePalaceLawDefinitionsQueryHandler.cs
```

### But métier

Permettre au Catalog de fournir des définitions de Lois du Palais.

Séparation cible :

```text
PalaceLawDefinition
→ Catalog
→ définition versionnée

ActivePalaceLaw
→ Game Engine
→ snapshot appliqué à une run
```

---

## 12. EventTemplate — intégration Application

### Fichiers ajoutés côté Application

```text
Application/EventTemplates/Dtos/EventTemplateDto.cs

Application/EventTemplates/Ports/IEventTemplateReadStore.cs

Application/EventTemplates/GetEventTemplateByKey/
→ GetEventTemplateByKeyQuery.cs
→ GetEventTemplateByKeyResponse.cs
→ GetEventTemplateByKeyQueryValidator.cs
→ GetEventTemplateByKeyQueryHandler.cs

Application/EventTemplates/ListActiveEventTemplates/
→ ListActiveEventTemplatesQuery.cs
→ ListActiveEventTemplatesResponse.cs
→ ListActiveEventTemplatesQueryHandler.cs
```

### But métier

Permettre au Catalog de fournir des templates d’événements.

Séparation cible :

```text
EventTemplate
→ Catalog
→ définition possible d’un événement

NodeEventOutcome / ResolvedEvent
→ Game Engine
→ résultat runtime dans une run
```

---

## 13. Tests unitaires ajoutés

Les tests unitaires couvrent :

```text
ValidationBehavior
Validators
Handlers
Mappings DTO
Ports mockés
Cas trouvé / non trouvé
ListActive
```

### PalaceLaws

```text
GetPalaceLawDefinitionByKeyQueryValidatorTests
PalaceLawDefinitionQueryHandlerTests
```

### EventTemplates

```text
GetEventTemplateByKeyQueryValidatorTests
EventTemplateQueryHandlerTests
```

### Résultat

```text
94 tests passent
```

---

## 14. Ce que ces tests prouvent

Les tests prouvent que :

```text
- les entrées externes invalides sont rejetées ;
- les handlers ne dépendent que de ports Application ;
- les read stores peuvent être substitués par mocks ;
- les DTOs exposent les bonnes données ;
- les cas null sont gérés proprement ;
- la logique Application est testable sans API et sans Infrastructure.
```

---

## 15. Problème rencontré et correction

Erreur rencontrée sur :

```text
ListActivePalaceLawDefinitionsQueryHandler
```

Cause :

```text
ListActivePalaceLawDefinitionsQuery
```

n’était pas correctement déclarée comme query publique implémentant :

```text
IQuery<ListActivePalaceLawDefinitionsResponse>
```

Correction :

```csharp
public sealed record ListActivePalaceLawDefinitionsQuery()
    : IQuery<ListActivePalaceLawDefinitionsResponse>;
```

Règle à retenir :

```text
Toutes les queries publiques consommées par MediatR doivent être publiques
et implémenter IRequest<TResponse> directement ou via IQuery<TResponse>.
```

---

## 16. Pourquoi on ne commit pas encore forcément

À ce stade, le Bloc 1 Application est bon.

Mais la PR complète doit encore intégrer :

```text
Infrastructure InMemory
DI Infrastructure
Endpoints API
Tests d’intégration API
```

La PR complète visée est :

```text
feat(catalog): add palace law and event template read flows
```

Elle ne doit pas seulement contenir le Domain et l’Application.

---

## 17. Prochaine étape

Prochain bloc :

```text
Bloc 2 — Infrastructure InMemory + DI
```

À ajouter :

```text
Infrastructure/ReadStores/InMemoryPalaceLawDefinitionReadStore.cs
Infrastructure/ReadStores/InMemoryEventTemplateReadStore.cs
Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs
```

Ces read stores implémenteront :

```text
IPalaceLawDefinitionReadStore
IEventTemplateReadStore
```

Ensuite :

```text
Bloc 3 — API endpoints + tests d’intégration
```

Routes cibles :

```text
GET /api/v2/catalog/palace-laws
GET /api/v2/catalog/palace-laws/{key}

GET /api/v2/catalog/event-templates
GET /api/v2/catalog/event-templates/{key}
```

---

## 18. Synthèse pédagogique

Ce que nous faisons peut être résumé ainsi :

```text
Domain
→ Je définis ce qu’est une Loi ou un EventTemplate.

Application
→ Je définis comment on demande une Loi ou un EventTemplate.

Validator
→ Je vérifie que la demande est valide.

Handler
→ J’exécute la demande.

Port
→ Je décris comment l’Application veut lire les données.

Infrastructure
→ Je fournit une implémentation concrète de cette lecture.

API
→ J’expose la demande au monde extérieur.
```

Si demain on remplace InMemory par PostgreSQL :

```text
Handler
→ inchangé

Query
→ inchangée

Validator
→ inchangé

API
→ inchangée

Seule Infrastructure change.
```

C’est précisément l’intérêt de la Clean Architecture et de l’inversion de dépendance.

---

## 19. Commandes de validation

Depuis `services/catalog` :

```bash
dotnet format Leds.Catalog.slnx
dotnet test Leds.Catalog.slnx
```

Puis :

```bash
cd ../game-engine
dotnet test Leds.GameEngine.slnx
```

---

## 20. Commit final visé après les prochains blocs

Quand Infrastructure + API seront intégrées et testées :

```bash
git add services/catalog docs/v2/follow-up/SUIVI_TECHNIQUE_ALPHA_0_0_4_CATALOG_CQRS_READ_FLOWS.md
git commit -m "feat(catalog): add palace law and event template read flows"
git push
```
