# L'épopée des silences — Suivi technique alpha-0.0.13

## PR — SelectReward Command + Run Reward State + Apply Effect

**Service concerné :** `services/game-engine`
**Version concernée :** `alpha-0.0.13`
**Type de PR :** feat / sélection et application de récompense
**Statut final :** validé si build et tests passent
**Diffusion :** confidentiel projet

---

## 1. Contexte

Le jalon `alpha-0.0.12` a introduit le domaine RewardOffer complet avec factory, repository, DTO et endpoint de consultation. Le joueur peut voir la récompense proposée après un combat, mais ne peut pas encore la sélectionner ni l'appliquer à sa run.

`alpha-0.0.13` expose la sélection de récompense, l'état de récompense en attente sur la run, et l'application des effets de la récompense choisie.

---

## 2. Problème initial

Avant cette PR :

```text
- pas de commande de sélection de récompense ;
- pas d'état HasPendingRewardOffer / PendingRewardOfferId sur Run ;
- pas d'application d'effet de récompense ;
- pas d'endpoint POST /rewards/select ;
- pas de tests pour le flux de sélection.
```

---

## 3. Objectif de la PR

Permettre au joueur de sélectionner une récompense et de voir son effet appliqué à la run :

```text
- SelectRewardCommand + Handler ;
- état de récompense en attente sur Run (HasPendingRewardOffer, PendingRewardOfferId) ;
- méthode Run.SetPendingRewardOffer(RewardOfferId) ;
- méthode Run.ClearPendingRewardOffer() ;
- méthode Run.ApplyRewardEffect(RewardChoice) ;
- endpoint POST /api/v2/runs/{runId}/rewards/select ;
- enregistrement du choix dans RewardOffer ;
- tests unitaires du handler ;
- tests d'intégration du endpoint.
```

---

## 4. Éléments ajoutés

### 4.1 Domaine Run

```text
src/Leds.GameEngine.Domain/Runs/Run.cs.cs
→ HasPendingRewardOffer : bool
→ PendingRewardOfferId : RewardOfferId?
→ SetPendingRewardOffer(RewardOfferId)
→ ClearPendingRewardOffer()
→ ApplyRewardEffect(RewardChoice)
```

### 4.2 Application

```text
src/Leds.GameEngine.Application/Rewards/SelectReward/
├── SelectRewardCommand.cs
├── SelectRewardCommandHandler.cs
├── SelectRewardCommandValidator.cs
└── SelectRewardResponse.cs
```

### 4.3 API Endpoint

```text
POST /api/v2/runs/{runId}/rewards/select
Body: { ChoiceId }
```

Ajouté dans `RewardsController.cs`.

### 4.4 DTOs

```text
src/Leds.GameEngine.Application/Rewards/Dtos/
├── RewardOfferDto.cs (enrichi)
└── RewardChoiceDto.cs (enrichi)
```

---

## 5. Détail du handler

`SelectRewardCommandHandler.Handle(...)` exécute les étapes suivantes :

```text
1. Charger la run depuis le repository ;
2. Valider que HasPendingRewardOffer est true ;
3. Charger la RewardOffer depuis le repository ;
4. rewardOffer.SelectChoice(choiceId) → valide le choix ;
5. run.ApplyRewardEffect(rewardOffer.Choices.Single(...)) ;
6. run.ClearPendingRewardOffer() ;
7. Persister rewardOffer et run ;
8. Retourner SelectRewardResponse.
```

---

## 6. État de récompense sur Run

Trois nouvelles méthodes sur `Run` :

```csharp
public void SetPendingRewardOffer(RewardOfferId rewardOfferId)
{
    // Valide que rewardOfferId n'est pas vide
    // Valide que HasPendingRewardOffer est false
    // Définit PendingRewardOfferId
}

public void ApplyRewardEffect(RewardChoice choice)
{
    // Application temporaire (placeholder pour alpha-0.1.5)
    // Marque la récompense comme appliquée
}

public void ClearPendingRewardOffer()
{
    // Valide que HasPendingRewardOffer est true
    // Remet PendingRewardOfferId à null
}
```

Le `RunDto` expose désormais :

```text
- HasPendingRewardOffer : bool
- PendingRewardOfferId : Guid?
```

---

## 7. Tests unitaires (5 tests)

```text
Handle_ShouldReturnFailure_WhenRunHasNoPendingOffer
Handle_ShouldReturnFailure_WhenChoiceDoesNotExist
Handle_ShouldSelectChoice_WhenChoiceIsValid
Handle_ShouldMarkRewardAsSelected_AfterSelection
Handle_ShouldApplyEffect_WhenChoiceIsValid
```

---

## 8. Tests d'intégration

Les tests d'intégration existants ont été enrichis pour valider le flux complet :

```text
- CompleteActiveCombatAsync sélectionne la première récompense après combat ;
- ResolveCurrentEvent_ShouldResolveNode_AfterCombatCompleted valide l'état final ;
- RoomBossProgressionEndpointTests parcourt la room complète avec récompenses ;
- ProgressRunEndpointTests intègre la sélection de récompense.
```

---

## 9. Fiabilisation des tests d'intégration

Les tests d'intégration ont été corrigés pour gérer :

```text
- les événements de type Elite (ResolvedEliteEventContent) ;
- les événements nécessitant un choix joueur (Npc, Merchant, Law, Curse) ;
- les événements non-Combat (Item, Rest, etc.) ;
- la complétion de combat avec récompense ;
- la sélection automatique de récompense dans CompleteActiveCombatAsync.
```

---

## 10. Correctifs intégrés

### 10.1 Handler ResolveCurrentEvent

Le pattern matching du handler a été corrigé pour accepter `ResolvedEliteEventContent` :

```csharp
var (enemyTemplateKey, _) = contentResult.Value switch
{
    ResolvedCombatEventContent c => (c.EnemyTemplateKey, c.RiskLevel),
    ResolvedEliteEventContent e => (e.EnemyTemplateKey, e.RiskLevel),
    _ => throw new DomainException("Expected combat or elite event content.")
};
```

### 10.2 Run d'intégration

`ResolveAndHandleCombatAsync` retourne désormais l'état le plus récent de la run (via GET) après complétion du combat et sélection de la récompense.

---

## 11. Critères de validation

```text
- SelectRewardCommand accessible via API REST ;
- le choix est validé et enregistré sur RewardOffer ;
- l'effet de la récompense est appliqué à la run ;
- l'offre en attente est nettoyée après sélection ;
- HasPendingRewardOffer reflète l'état réel ;
- 5 tests unitaires SelectReward passent ;
- 26 tests d'intégration passent (tous) ;
- 224 tests unitaires passent (tous).
```

---

## 12. Commandes de validation

```bash
dotnet test services/game-engine/Leds.GameEngine.slnx
```

---

## 13. Suite recommandée

Prochain jalon :

```text
alpha-0.1.0
→ First Backend Playable Slice
→ boucle complète : run → room → combat → reward → progression → boss
```

Le jalon `alpha-0.0.13` complète la dernière brique manquante pour une boucle jouable :

```text
✅ Démarrer une run
✅ Générer une room initiale
✅ Choisir un node
✅ Résoudre un événement (Combat, Elite, Npc, etc.)
✅ Démarrer un combat
✅ Exécuter une action de combat
✅ Terminer le combat
✅ Recevoir une récompense
✅ Sélectionner la récompense
✅ Résoudre le node
✅ Progresser vers la couche suivante
✅ Atteindre le boss de room
✅ Résoudre le boss
✅ Terminer la room
```
