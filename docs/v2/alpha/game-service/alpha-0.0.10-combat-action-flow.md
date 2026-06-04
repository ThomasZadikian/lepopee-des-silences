# L'épopée des silences — Suivi technique alpha-0.0.10

## PR — Combat Action Flow

**Service concerné :** `services/game-engine`
**Version concernée :** `alpha-0.0.10`
**Type de PR :** feat / exposition boucle d'action de combat
**Statut final :** validé si build et tests passent
**Diffusion :** confidentiel projet

---

## 1. Contexte

Le jalon `alpha-0.0.9` a introduit la création d'une `CombatInstance` depuis la résolution d'un événement `Combat` ou `Elite`. Une fois le combat créé, le joueur devait pouvoir interagir avec lui.

`alpha-0.0.10` expose la première boucle d'action de combat côté backend, permettant au joueur de soumettre une action `BasicAttack`, de voir le combat évoluer tour par tour, et d'atteindre un état terminal (victoire ou défaite).

---

## 2. Problème initial

Avant cette PR, un combat pouvait être créé mais aucune action n'était accessible :

```text
- pas de endpoint d'action combat ;
- pas de SubmitCombatActionCommand ;
- pas de gestion des tours ;
- pas de résolution de dégâts côté applicatif ;
- pas de fin de combat déclenchée par les actions.
```

---

## 3. Objectif de la PR

Créer la première boucle jouable de combat :

```text
- SubmitCombatActionCommand + Handler ;
- BasicAttack comme action minimale ;
- boucle de résolution incluant les actions ennemies automatiques ;
- détection de fin de combat (victoire / défaite) ;
- création d'une RewardOffer en fin de combat victorieux ;
- exposition via endpoint API REST ;
- tests d'intégration complets.
```

---

## 4. Éléments ajoutés

### 4.1 Command et Handler

```text
src/Leds.GameEngine.Application/Combats/SubmitCombatAction/
├── SubmitCombatActionCommand.cs
├── SubmitCombatActionCommandHandler.cs
├── SubmitCombatActionResponse.cs
└── SubmitCombatActionCommandValidator.cs
```

### 4.2 DTOs

```text
src/Leds.GameEngine.Application/Combats/Dtos/
└── CombatActionResultDto.cs
```

### 4.3 API Endpoint

```text
POST /api/v2/runs/{runId}/combats/{combatId}/actions
Body: { ActorId, TargetId, ActionType }
```

Ajouté dans `RunsController.cs`.

### 4.4 Tests d'intégration

```text
tests/Leds.GameEngine.IntegrationTests/Runs/
├── RunIntegrationTestBase.cs (méthode CompleteActiveCombatAsync)
└── ResolveCurrentEventEndpointTests.cs (combat flow tests)
```

---

## 5. Détail du handler

`SubmitCombatActionCommandHandler.Handle(...)` exécute les étapes suivantes :

```text
1. Charger la run depuis le repository ;
2. Valider le statut Active ;
3. Valider la présence d'un combat actif ;
4. Charger la CombatInstance depuis le repository ;
5. Valider le combatId correspond à la run ;
6. Valider le type d'action (BasicAttack uniquement) ;
7. Soumettre l'action via combat.SubmitAction(...) ;
8. Résoudre automatiquement les actions ennemies dans la boucle ;
9. Si combat terminé :
   a. run.CompleteActiveCombat(...) → résout le node interne ;
   b. Créer une RewardOffer (source Combat ou RoomBoss) ;
   c. run.SetPendingRewardOffer(...) ;
10. Sauvegarder combat et run ;
11. Retourner le DTO de résultat.
```

---

## 6. Boucle d'action ennemie

Après chaque action joueur, le handler résout automatiquement les actions ennemies :

```text
Tant que le prochain acteur est un ennemi :
→ Créer une BasicAttack sur le joueur
→ Soumettre l'action
→ Si combat terminé, sortir de la boucle
```

Cette approche permet au backend de rester serveur-autoritaire sans nécessiter de polling client.

---

## 7. Fin de combat

Deux issues possibles :

```text
Victoire du joueur :
→ run.CompleteActiveCombat(combatId)
→ ResolveCurrentEvent() interne (état → NodeResolved)
→ RewardOffer créée
→ run.SetPendingRewardOffer(...)

Défaite du joueur :
→ run.FailActiveCombat(combatId, UtcNow)
→ Statut run → Failed
```

---

## 8. RewardOffer associée

La `RewardOfferFactory` crée une offre de récompense basée sur :

```text
- source : RewardSource.Combat ou RewardSource.RoomBoss
- riskLevel : valeur fixe 25 (temporaire)
```

L'offre est persistée dans `IRewardOfferRepository` et rattachée à la run.

---

## 9. Tests d'intégration

Tests ajoutés ou enrichis :

```text
ResolveCurrentEvent_ShouldStartCombat_WhenEventIsCombat
→ vérifie ActiveCombatId non null après résolution

ResolveCurrentEvent_ShouldResolveNode_AfterCombatCompleted
→ vérifie état NodeResolved après combat + reward

MoveToNextRoomEndpointTests (via base)
→ utilise ResolveAndHandleCombatAsync pour le flow complet

RoomBossProgressionEndpointTests (via base)
→ boucle complète avec combat + reward + progression

ProgressRunEndpointTests (via base)
→ vérifie progression après combat résolu
```

---

## 10. Base commune de test

`RunIntegrationTestBase` a été introduite pour centraliser les helpers de test :

```text
ResolveAndHandleCombatAsync(runId)
→ resolve, combat complet, reward, choix événement si nécessaire

CompleteActiveCombatAsync(runId, combatId)
→ combat loop jusqu'à victoire, reward selection

ResolveEventChoiceIfRequiredAsync(runId)
→ choix automatique pour événements Npc/Merchant/Law/Curse

StartRunAsync()
→ run standard avec seed aléatoire
```

---

## 11. Fiabilisation des tests

Les tests d'intégration ont été rendus résilients face à la génération aléatoire de seed :

```text
- ResolveCurrentEvent_ShouldStartCombat_WhenEventIsCombat
  → skip si le premier node n'est pas Combat

- ResolveEventChoiceIfRequiredAsync
  → détecte les événements nécessitant un choix joueur
  → applique un choix par défaut (listen / trade / accept-law / accept-curse)
  → ne tente pas de choix pour les types non supportés
```

---

## 12. Correctifs intégrés

Le handler `ResolveCurrentEventCommandHandler` a été corrigé pour accepter `ResolvedEliteEventContent` en plus de `ResolvedCombatEventContent` :

```text
→ pattern matching sur les deux types de contenu
→ extraction unifiée de EnemyTemplateKey
```

---

## 13. Critères de validation

```text
- SubmitCombatActionCommand accessible via API REST ;
- BasicAttack inflige des dégâts et modifie l'état du combat ;
- les actions ennemies sont résolues automatiquement ;
- le combat peut se terminer par victoire ou défaite ;
- une RewardOffer est créée après victoire ;
- tous les tests d'intégration passent (26 tests) ;
- tous les tests unitaires passent (224 tests).
```

---

## 14. Commandes de validation

```bash
dotnet test services/game-engine/Leds.GameEngine.slnx
```

---

## 15. Suite recommandée

Prochain jalon :

```text
alpha-0.0.11
→ Reward Offer Foundation : consolider le domaine RewardOffer
```

```text
alpha-0.0.12
→ Reward Offer repository, DTO, tests
```

```text
alpha-0.0.13
→ SelectReward command, application d'effet, tests
```
