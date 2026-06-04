# L'épopée des silences — Suivi technique alpha-0.0.11

## PR — Event Content Resolution Pipeline

**Service concerné :** `services/game-engine`
**Version concernée :** `alpha-0.0.11`
**Type de PR :** feat / pipeline de résolution de contenu événementiel typé
**Statut final :** validé si build et tests passent
**Diffusion :** confidentiel projet

---

## 1. Contexte

Le pipeline de résolution de contenu événementiel a été introduit dans `alpha-0.0.6` comme une infrastructure de base (`IEventContentResolutionStrategy`, `EventContentResolver`). Le jalon `alpha-0.0.11` complète ce pipeline en ajoutant les stratégies de résolution pour chaque famille d'événement et en connectant le tout au handler `ResolveCurrentEventCommandHandler`.

---

## 2. Problème initial

Avant cette PR, le pipeline de résolution de contenu existait mais n'était pas connecté au flux de résolution d'événement courant :

```text
- EventContentResolver non injecté dans ResolveCurrentEventCommandHandler ;
- aucune stratégie de résolution concrète pour les types d'événement ;
- le handler créait un combat sans résoudre le contenu via le pipeline ;
- pas de distinction entre contenu Combat et contenu Elite.
```

---

## 3. Objectif de la PR

Connecter le pipeline de résolution de contenu événementiel au flux de résolution de l'événement courant :

```text
- injecter IEventContentResolver dans ResolveCurrentEventCommandHandler ;
- implémenter EventContentResolutionContext complet ;
- stratégie de résolution pour Combat et Elite ;
- distinction ResolvedCombatEventContent / ResolvedEliteEventContent ;
- extraction EnemyTemplateKey depuis le contenu résolu ;
- création de CombatInstance à partir du template ennemi résolu.
```

---

## 4. Éléments modifiés

### 4.1 Nouvelles stratégies de résolution

```text
src/Leds.GameEngine.Infrastructure/Events/Resolution/
├── CombatEventContentResolutionStrategy.cs
└── EventContentResolutionStrategy.cs (dispatcher interne)
```

### 4.2 Contrats applicatifs

```text
src/Leds.GameEngine.Application/Events/Contracts/
├── ResolvedNodeEventContent.cs (base abstraite)
├── ResolvedCombatEventContent.cs
├── ResolvedEliteEventContent.cs
├── ResolvedRestEventContent.cs
├── ResolvedItemEventContent.cs
├── ResolvedNpcEventContent.cs
├── ResolvedMerchantEventContent.cs
├── ResolvedLawEventContent.cs
├── ResolvedCurseEventContent.cs
├── ResolvedRareEventContent.cs
└── ResolvedEventContentKind.cs
```

### 4.3 Handler modifié

```text
ResolveCurrentEventCommandHandler.cs
→ résolution de contenu via EventContentResolver
→ pattern matching sur ResolvedCombatEventContent / ResolvedEliteEventContent
→ création de combat depuis EnemyTemplateKey
→ gestion des risques (RiskLevel)
```

---

## 5. Détail du flux

```text
1. Dispatcher de résolution → NodeEventResolutionResult
2. Si CombatStarted ou EliteEncounterStarted :
   a. Créer EventContentResolutionContext (Seed, RoomType, etc.)
   b. Résoudre le contenu via EventContentResolver
   c. Pattern matching : ResolvedCombatEventContent ou ResolvedEliteEventContent
   d. Récupérer EnemyTemplateKey depuis le contenu
   e. Charger EnemyTemplateSnapshot depuis le Catalog
   f. Créer CombatInstance via CombatInstanceFactory
   g. Persister le combat
   h. run.SetActiveCombat(combat.Id)
3. Sinon : run.ResolveCurrentEvent()
4. Retourner ResolveCurrentEventResponse
```

---

## 6. Stratégie CombatEventContentResolutionStrategy

```text
Types supportés : Combat, Elite

Comportement :
→ Charge EventTemplate depuis le Catalog (clé "event-combat-shadow-v1")
→ Charge EnemyTemplate depuis le Catalog (clé "enemy-shadow-v1")
→ Si event type = Elite → retourne ResolvedEliteEventContent
→ Sinon → retourne ResolvedCombatEventContent
```

Les deux types de contenu partagent la même structure (EnemyTemplateKey, RiskLevel) mais diffèrent par leur `Kind` (`Combat` vs `Elite`), permettant au handler de les distinguer si nécessaire.

---

## 7. Injection DI

```csharp
services.AddScoped<IEventContentResolver, EventContentResolver>();
services.AddScoped<IEventContentResolutionStrategy, CombatEventContentResolutionStrategy>();
```

Les autres stratégies (Rest, Item, Npc, etc.) sont prévues pour les jalons ultérieurs.

---

## 8. Critères de validation

```text
- ResolveCurrentEventCommandHandler utilise EventContentResolver ;
- contenu Combat correctement résolu → CombatInstance créée ;
- contenu Elite correctement résolu → ResolvedEliteEventContent accepté ;
- EnemyTemplateKey extrait et utilisé pour la création de combat ;
- tests unitaires du pipeline de résolution ;
- tests d'intégration du flux complet.
```
