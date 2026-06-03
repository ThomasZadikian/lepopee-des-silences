# L’épopée des silences — Suivi technique alpha-0.0.6

## PR — Typed Event Content Resolution Pipeline

**Branche cible :** `v2/develop`
**Service concerné :** `services/game-engine`
**Version concernée :** `alpha-0.0.6`
**Type de PR :** feature Game Engine / Application pipeline / Event content resolution / Strategy pattern
**Commit recommandé :** `feat(game-engine): introduce typed event content resolution pipeline`
**Statut final :** à compléter après validation des tests
**Diffusion :** confidentiel projet

---

## 1. Contexte de la PR

Cette PR ouvre le jalon `alpha-0.0.6`.

Le jalon précédent, `alpha-0.0.5`, a permis de poser une fondation Markov réelle dans le Game Engine :

```text
- moteur Markov déterministe ;
- versioning de génération ;
- sélection du RoomType par Markov ;
- contrats Catalog côté Game Engine ;
- sélection du NodeEventType par Markov ;
- respect des contraintes métier de room et de node.
```

La génération sait désormais produire des rooms, des nodes et des familles d’événements.

Cependant, un `NodeEventType` ne suffit pas encore à représenter un événement réellement exploitable par le runtime.

Exemple :

```text
NodeEventType.Combat
```

ne dit pas encore :

```text
- quel EventTemplate est utilisé ;
- quel EnemyTemplate est associé ;
- quel niveau de risque est transmis ;
- quel contenu sera utilisé plus tard pour créer un CombatInstance.
```

Cette PR introduit donc la couche suivante du pipeline : la résolution typée du contenu événementiel.

---

## 2. Objectif de la PR

L’objectif est de transformer une famille d’événement générée en contenu applicatif résolu, typé et exploitable par les futures couches runtime.

Flux cible :

```text
NodeEventType
+ RoomType
+ Seed
+ RoomDepth
+ NodeDepth
+ EventOrder
+ RiskLevel
+ RewardProfile
→ IEventContentResolver
→ IEventContentResolutionStrategy
→ ResolvedNodeEventContent typé
```

La PR ne crée pas encore les agrégats runtime finaux.

Elle prépare le passage vers :

```text
ResolvedCombatEventContent
→ CombatInstance

ResolvedItemEventContent
→ RewardOffer

ResolvedPalaceLawEventContent
→ ActivePalaceLaw

ResolvedNpcEventContent
→ NpcInteraction

ResolvedRareEventContent
→ RareEventOutcome
```

---

## 3. Décision d’architecture

La décision majeure de cette PR est de ne pas créer un resolver générique basé sur un dictionnaire de références non typées.

L’approche retenue est un pipeline typé par stratégie.

Cela évite une dette d’architecture et respecte la volonté projet :

```text
Quand une brique est structurante, elle doit être conçue proprement dès le départ.
```

La PR introduit donc :

```text
- un contrat abstrait commun ;
- des contenus résolus spécialisés ;
- un resolver orchestrateur ;
- une stratégie par famille d’événement.
```

Ce design est conçu pour rester valable lorsque les modules Combat, Reward, Palace Laws, NPC, Merchant, Rare Events et Narrative seront enrichis.

---

## 4. Positionnement Clean Architecture

La PR respecte la Clean Architecture.

Répartition par couche :

```text
GameEngine.Application
→ contrats applicatifs de résolution ;
→ port IEventContentResolver ;
→ interface IEventContentResolutionStrategy ;
→ orchestrateur EventContentResolver.

GameEngine.Infrastructure
→ stratégies concrètes actuelles ;
→ consommation temporaire du CatalogContentGateway ;
→ placeholders contrôlés pour les contenus non encore portés par Catalog.

GameEngine.Domain
→ inchangé ;
→ ne dépend ni d’Application, ni d’Infrastructure, ni d’API, ni de Catalog.
```

La couche `Application` ne contient pas de logique d’infrastructure.

La couche `Infrastructure` implémente les stratégies concrètes et dépend des ports applicatifs.

La couche `Domain` reste pure.

---

## 5. Respect CQRS

Cette PR ne modifie pas encore les controllers ni les handlers.

Elle prépare le futur use case :

```text
ResolveNodeEventCommand
→ ResolveNodeEventCommandHandler
→ IEventContentResolver
```

Le pipeline de résolution est donc prêt à être appelé depuis un handler CQRS, sans déplacer la logique dans l’API.

Aucun controller n’est modifié.

Aucun contournement MediatR n’est introduit.

La logique de résolution reste injectable, testable et orchestrable depuis Application.

---

## 6. Respect de l’architecture microservices

Cette PR ne transforme pas le Game Engine en service propriétaire de tous les domaines.

Le Game Engine reste propriétaire du runtime gameplay :

```text
- run ;
- room ;
- node ;
- événement runtime ;
- combat runtime futur ;
- rewards runtime futurs ;
- lois actives futures ;
- résolution serveur-autoritaire.
```

Les services périphériques conservent leurs responsabilités :

```text
Catalog
→ templates versionnés : ennemis, skills, items, events, lois, NPC plus tard.

Player
→ progression durable, inventaire permanent, compagnons débloqués, statistiques cumulées.

Identity
→ comptes, authentification, JWT, MFA, rôles.

Audit/GDPR
→ audit, consentements, export, anonymisation.

Leaderboard
→ projections et classements.
```

La communication avec Catalog passe toujours par le port applicatif existant :

```text
ICatalogContentGateway
```

Aucune dépendance directe à `Leds.Catalog.*` n’est ajoutée dans le Game Engine.

---

## 7. Fichiers ajoutés

### Application — Contracts

```text
src/Leds.GameEngine.Application/Events/Contracts/EventContentResolutionContext.cs
src/Leds.GameEngine.Application/Events/Contracts/ResolvedEventContentKind.cs
src/Leds.GameEngine.Application/Events/Contracts/ResolvedNodeEventContent.cs
src/Leds.GameEngine.Application/Events/Contracts/ResolvedCombatEventContent.cs
src/Leds.GameEngine.Application/Events/Contracts/ResolvedEliteEventContent.cs
src/Leds.GameEngine.Application/Events/Contracts/ResolvedItemEventContent.cs
src/Leds.GameEngine.Application/Events/Contracts/ResolvedPalaceLawEventContent.cs
src/Leds.GameEngine.Application/Events/Contracts/ResolvedCurseEventContent.cs
src/Leds.GameEngine.Application/Events/Contracts/ResolvedNpcEventContent.cs
src/Leds.GameEngine.Application/Events/Contracts/ResolvedRestEventContent.cs
src/Leds.GameEngine.Application/Events/Contracts/ResolvedMerchantEventContent.cs
src/Leds.GameEngine.Application/Events/Contracts/ResolvedRareEventContent.cs
```

### Application — Ports

```text
src/Leds.GameEngine.Application/Events/Ports/IEventContentResolver.cs
```

### Application — Resolution

```text
src/Leds.GameEngine.Application/Events/Resolution/IEventContentResolutionStrategy.cs
src/Leds.GameEngine.Application/Events/Resolution/EventContentResolver.cs
```

### Infrastructure — Strategies

```text
src/Leds.GameEngine.Infrastructure/Events/Resolution/CombatEventContentResolutionStrategy.cs
src/Leds.GameEngine.Infrastructure/Events/Resolution/ItemEventContentResolutionStrategy.cs
src/Leds.GameEngine.Infrastructure/Events/Resolution/PalaceLawEventContentResolutionStrategy.cs
src/Leds.GameEngine.Infrastructure/Events/Resolution/NpcEventContentResolutionStrategy.cs
src/Leds.GameEngine.Infrastructure/Events/Resolution/RestEventContentResolutionStrategy.cs
src/Leds.GameEngine.Infrastructure/Events/Resolution/MerchantEventContentResolutionStrategy.cs
src/Leds.GameEngine.Infrastructure/Events/Resolution/RareEventContentResolutionStrategy.cs
```

### Tests

```text
tests/Leds.GameEngine.UnitTests/Events/EventContentResolverTests.cs
```

---

## 8. Fichiers modifiés

```text
src/Leds.GameEngine.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs
```

La DI enregistre désormais :

```text
IEventContentResolver → EventContentResolver

IEventContentResolutionStrategy → CombatEventContentResolutionStrategy
IEventContentResolutionStrategy → ItemEventContentResolutionStrategy
IEventContentResolutionStrategy → PalaceLawEventContentResolutionStrategy
IEventContentResolutionStrategy → NpcEventContentResolutionStrategy
IEventContentResolutionStrategy → RestEventContentResolutionStrategy
IEventContentResolutionStrategy → MerchantEventContentResolutionStrategy
IEventContentResolutionStrategy → RareEventContentResolutionStrategy
```

---

## 9. Modèle introduit

### 9.1 EventContentResolutionContext

`EventContentResolutionContext` porte les informations nécessaires à la résolution d’un événement :

```text
Seed
RoomType
RoomDepth
NodeDepth
EventOrder
EventType
RiskLevel
RewardProfile
```

Ce contexte est volontairement applicatif.

Il représente les données nécessaires au pipeline de résolution, sans dépendre de l’API ou de l’infrastructure.

---

### 9.2 ResolvedNodeEventContent

`ResolvedNodeEventContent` est le contrat abstrait commun des contenus résolus.

Il contient :

```text
Kind
EventTemplateKey
EventTemplateVersion
Tags
```

Il ne remplace pas les futurs agrégats métier runtime.

Il sert de pont typé entre :

```text
génération de node event
→ résolution de contenu
→ futurs modules runtime spécialisés
```

---

### 9.3 Contenus typés

Les contenus spécialisés introduits sont :

```text
ResolvedCombatEventContent
ResolvedEliteEventContent
ResolvedItemEventContent
ResolvedPalaceLawEventContent
ResolvedCurseEventContent
ResolvedNpcEventContent
ResolvedRestEventContent
ResolvedMerchantEventContent
ResolvedRareEventContent
```

Chaque type représente une famille d’événement différente.

Cette séparation évite un modèle générique fourre-tout et prépare directement les futurs modules spécialisés.

---

## 10. Stratégies de résolution

La PR introduit le pattern Strategy pour éviter un gros `switch` central difficile à maintenir.

Chaque stratégie déclare explicitement les types d’événements qu’elle supporte :

```text
CombatEventContentResolutionStrategy
→ Combat, Elite

ItemEventContentResolutionStrategy
→ Item

PalaceLawEventContentResolutionStrategy
→ Law, Curse

NpcEventContentResolutionStrategy
→ Npc

RestEventContentResolutionStrategy
→ Rest

MerchantEventContentResolutionStrategy
→ Merchant

RareEventContentResolutionStrategy
→ Rare
```

L’orchestrateur `EventContentResolver` :

```text
- valide le contexte ;
- rejette les événements hors pipeline standard ;
- trouve la stratégie correspondante ;
- rejette les cas sans stratégie ;
- rejette les cas ambigus ;
- délègue la résolution.
```

---

## 11. Événements exclus du pipeline standard

Certains événements ne doivent pas être résolus par le pipeline standard des événements planifiés.

Sont exclus :

```text
Memory
RoomBoss
FinalBoss
```

Raisons :

```text
Memory
→ n’est pas planifié dans les nodes standards ;
→ apparition future à la résolution ou via logique dédiée.

RoomBoss
→ créé par la génération spécifique du boss de room.

FinalBoss
→ devra être résolu par un pipeline dédié au boss final.
```

---

## 12. Relation avec Catalog

Les stratégies utilisent actuellement le port :

```text
ICatalogContentGateway
```

Elles récupèrent des snapshots Catalog déjà disponibles :

```text
EventTemplateSnapshot
EnemyTemplateSnapshot
ItemTemplateSnapshot
PalaceLawDefinitionSnapshot
```

Pour les contenus non encore portés par Catalog, des clés placeholder contrôlées sont utilisées :

```text
npc-placeholder-v1
npc-interaction-placeholder-v1
merchant-placeholder-v1
rare-event-placeholder-v1
```

Ces placeholders ne sont pas une architecture temporaire.

Ils signalent simplement que les sources Catalog correspondantes seront branchées ultérieurement sans changer la structure du pipeline.

---

## 13. Ce que cette PR valide

Cette PR valide que le Game Engine dispose désormais d’un pipeline événementiel typé capable de transformer un `NodeEventType` en contenu applicatif résolu.

Elle valide :

```text
- séparation des stratégies par famille d’événement ;
- modèle de sortie typé ;
- orchestration applicative testable ;
- consommation indirecte de Catalog via port ;
- rejet des événements hors pipeline standard ;
- préparation des futurs modules runtime ;
- respect de Clean Architecture ;
- respect CQRS ;
- absence de couplage direct avec les services périphériques.
```

---

## 14. Ce que cette PR ne fait pas encore

Cette PR ne crée pas encore :

```text
- CombatInstance ;
- CombatantSnapshot ;
- DamageResolver ;
- RewardOffer ;
- RewardChoice ;
- ActivePalaceLaw ;
- NpcRuntimeState ;
- NpcAttitudeResolver ;
- MerchantInventory ;
- RareEventOutcome ;
- TomeWriter ;
- Event Store ;
- endpoint API de résolution.
```

Ces éléments ne sont pas oubliés.

Ils sont volontairement séparés parce qu’ils représentent des sous-domaines runtime complets.

La PR actuelle leur prépare une entrée propre et typée.

---

## 15. Pourquoi cette PR n’est pas un placeholder

La PR introduit une structure finale :

```text
IEventContentResolver
→ EventContentResolver
→ IEventContentResolutionStrategy
→ stratégies spécialisées
→ contenus résolus typés
```

Cette structure ne sera pas remplacée.

Les futures PR enrichiront les stratégies et brancheront les vrais modules runtime, mais l’architecture de résolution restera valable.

---

## 16. Tests ajoutés

Les tests couvrent :

```text
- résolution Combat ;
- résolution Elite ;
- résolution Item ;
- résolution PalaceLaw ;
- résolution Curse ;
- résolution Npc ;
- résolution Rest ;
- résolution Merchant ;
- résolution Rare ;
- rejet Memory ;
- rejet RoomBoss ;
- rejet FinalBoss ;
- rejet si seed vide ;
- rejet si rewardProfile vide.
```

Les tests vérifient aussi les types concrets retournés, par exemple :

```text
Combat
→ ResolvedCombatEventContent

Elite
→ ResolvedEliteEventContent

Law
→ ResolvedPalaceLawEventContent

Curse
→ ResolvedCurseEventContent
```

---

## 17. Validation à effectuer

Commandes à exécuter :

```bash
dotnet format services/game-engine/Leds.GameEngine.slnx
dotnet test services/game-engine/Leds.GameEngine.slnx
dotnet test services/catalog/Leds.Catalog.slnx
dotnet test packages/shared-building-blocks/Leds.SharedBuildingBlocks.slnx
```

Résultat attendu :

```text
Tous les tests passent.
```

---

## 18. Vérifications architecture

Vérifier que `Domain` reste pur :

```powershell
Get-ChildItem services/game-engine/src/Leds.GameEngine.Domain -Recurse -Filter *.csproj |
  Select-String "Application|Infrastructure|Api|Catalog"
```

Vérifier que `Application` ne dépend pas d’Infrastructure :

```powershell
Get-ChildItem services/game-engine/src/Leds.GameEngine.Application -Recurse -Filter *.csproj |
  Select-String "Infrastructure"
```

Vérifier l’absence de couplage direct au service Catalog :

```powershell
Get-ChildItem services/game-engine -Recurse -Filter *.csproj |
  Select-String "Leds.Catalog"
```

Résultat attendu :

```text
Aucune dépendance interdite.
```

---

## 19. Risques maîtrisés

### Risque : Application devient un fourre-tout

Réponse :

```text
Application contient uniquement les contrats de résolution, les ports et l’orchestrateur.
Les vrais agrégats runtime seront placés dans les modules Domain dédiés.
```

### Risque : couplage avec Catalog

Réponse :

```text
Les stratégies consomment ICatalogContentGateway.
Aucune entité Catalog n’est référencée directement.
```

### Risque : gros switch central

Réponse :

```text
Le pipeline repose sur IEventContentResolutionStrategy.
Chaque famille d’événement a sa propre stratégie.
```

### Risque : modèle jetable

Réponse :

```text
Les contenus résolus typés constituent un pont stable.
Ils ne remplacent pas les futurs agrégats, ils les alimenteront.
```

### Risque : violation CQRS

Réponse :

```text
Aucun controller n’est modifié.
Le pipeline est prêt à être consommé par un futur command handler.
```

---

## 20. Suite recommandée

Suite logique immédiate :

```text
feat(game-engine): expose node event resolution use case
```

Objectif :

```text
Créer ResolveNodeEventCommand et ResolveNodeEventCommandHandler.
Brancher le pipeline de contenu au moment de la résolution effective d’un événement.
```

Ensuite :

```text
feat(game-engine): introduce combat runtime domain
```

Objectif :

```text
Créer CombatInstance, CombatantSnapshot, CombatAction et premiers invariants de combat.
```

Puis :

```text
feat(game-engine): start combat from resolved event content
```

Objectif :

```text
Transformer un ResolvedCombatEventContent en CombatInstance serveur-autoritaire.
```

---

## 21. Commit recommandé

```text
feat(game-engine): introduce typed event content resolution pipeline
```

---

## 22. Conclusion

Cette PR ouvre `alpha-0.0.6` avec une brique structurante du Game Engine.

La génération sait désormais produire des types d’événements, et le nouveau pipeline permet de résoudre ces types vers des contenus applicatifs typés.

La structure respecte les principes du projet :

```text
- Clean Architecture ;
- CQRS ;
- serveur-autoritaire ;
- découpage par responsabilités ;
- dépendance aux services périphériques uniquement par ports ;
- absence de modèle jetable ;
- préparation des modules runtime futurs.
```

Cette PR constitue donc le socle de la future résolution effective des événements, avant l’introduction des combats, récompenses, lois actives, PNJ et événements rares runtime.
