# Game Engine Reward Generation with RewardPowerMultiplier

## Objectif

Utiliser le `RewardPowerMultiplier` pour scaler les valeurs des rewards lors de leur génération.

## Architecture

```
CombatRiskProfileResolver (calcule RewardPowerMultiplier)
    ↓
RewardOfferFactory (utilise le multiplicateur pour scaler les rewards)
    ↓
RewardPowerScaler (applique le scaling de manière déterministe)
    ↓
RewardOffer (contient les options scalées)
```

## Comment ça fonctionne

### 1. Source du RewardPowerMultiplier

Le `RewardPowerMultiplier` est calculé par `CombatRiskProfileResolver` à partir du type d'événement et du niveau de risque :

```
riskDelta = max(0, actualRisk - baseRisk)
multiplier = clamp(1.0 + riskDelta / 100, 1.0, 1.75)
```

Les valeurs de baseRisk par tier :
- Normal : 20
- Rare : 30
- Elite : 35
- RoomBoss : 50
- FinalBoss : 70

### 2. Application

Le multiplicateur est appliqué **une seule fois**, à la génération de la `RewardOffer`, dans `RewardOfferFactory.CreateCombatRewardOffer`.

Flow :
1. `RewardOfferFactory` appelle `CombatRiskProfileResolver.Resolve(eventType, riskLevel)`
2. Le `RewardPowerMultiplier` est extrait du profil de risque
3. `RewardPowerScaler.ScaleAmount(baseAmount, multiplier)` calcule les montants finaux
4. Les options de reward sont créées avec les montants scalés

### 3. Stats impactées

Les montants des rewards Heal sont scalés par le multiplicateur.

### 4. Bornes de sécurité

| Borne | Valeur |
|---|---|
| MinMultiplier | 0.5 |
| MaxMultiplier | 3.0 |

Le montant final est toujours >= 1.

## Exemples

| Tier | BaseHeal | Multiplier | FinalHeal |
|---|---|---|---|
| Standard | 12 | 1.0 | 12 |
| Standard | 12 | 1.25 | 15 |
| Standard | 12 | 1.5 | 18 |
| Elite | 42 | 1.0 | 42 |
| Elite | 42 | 1.5 | 63 |
| Boss | 70 | 1.0 | 70 |
| Boss | 70 | 1.25 | 88 |

## Non-objectifs

- Pas d'inventaire permanent
- Pas de boutique complète
- Pas de nouvelles mécaniques de reward
- Pas de random non déterministe
