# Combat Risk Scaling — Game Engine 0.1.9

## Purpose

Every combat node on the map carries a `RiskLevel` (0–100). From version 0.1.9 onward,
that number drives two linked multipliers:

* **DifficultyMultiplier** — future use: scale enemy stats so harder nodes feel harder.
* **RewardPowerMultiplier** — attached to every `RewardOffer` so item-generation pipelines
  can allocate a proportionally larger power budget.

The multipliers are always equal and are computed by the central
`CombatRiskProfileResolver` service.

---

## Formula

```
riskDelta              = max(0, actualRisk − baseRisk)
difficultyMultiplier   = clamp(1.0 + riskDelta / 100.0,  1.0,  1.75)
rewardPowerMultiplier  = difficultyMultiplier
```

* **actualRisk** — the `MapNode.RiskLevel` value (0–100).
* **baseRisk** — the reference risk for the combat tier (see table below).
* Delta is never negative: a node easier than the base risk yields multiplier 1.00.
* The maximum multiplier is **1.75** regardless of delta.

---

## Combat Tiers and Base Risk

| Tier      | NodeEventType | BaseRisk |
|-----------|---------------|---------|
| Normal    | Combat        | 20      |
| Rare      | Rare          | 30      |
| Elite     | Elite         | 35      |
| RoomBoss  | RoomBoss      | 50      |
| FinalBoss | FinalBoss     | 70      |

---

## Risk Bands

The `RiskBand` groups the **actual** risk level into a qualitative label:

| Band     | ActualRisk range |
|----------|-----------------|
| Low      | 0 – 24          |
| Moderate | 25 – 49         |
| High     | 50 – 74         |
| Critical | 75 – 100        |

---

## Examples

| Tier     | BaseRisk | ActualRisk | RiskDelta | Multiplier | RiskBand |
|----------|----------|------------|-----------|------------|----------|
| Normal   | 20       | 20         | 0         | 1.00       | Moderate |
| Normal   | 20       | 100        | 80        | **1.75**   | Critical |
| Rare     | 30       | 70         | 40        | 1.40       | High     |
| Elite    | 35       | 35         | 0         | 1.00       | Moderate |
| Elite    | 35       | 75         | 40        | 1.40       | Critical |
| RoomBoss | 50       | 90         | 40        | 1.40       | Critical |

> **Reading example:** An Elite node with ActualRisk 75 has a delta of 40
> (75 − 35 = 40), producing a multiplier of **1.40**.
> The combat is ~40% harder than a baseline Elite, and the `RewardOffer` carries
> `RewardPowerMultiplier = 1.40`.  
> A future item-generator can use this value to grant ~40% more stat budget or affixes.

---

## Non-combat nodes

The scaling system only applies to:
`Combat`, `Rare`, `Elite`, `RoomBoss`, `FinalBoss`.

Nodes of type `Item`, `Rest`, `Npc`, `Law`, `Merchant`, `Curse`, and `Memory` are not
combat encounters and must not be passed to `ICombatRiskProfileResolver.Resolve()`.
The method throws `ArgumentException` for unsupported types.

---

## Where to find the data

After a victorious combat, the resulting `RewardOffer` carries:

```csharp
offer.CombatScaling   // CombatRiskProfile (never null for combat offers)
  .Tier               // CombatTier enum
  .BaseRisk           // int
  .ActualRisk         // int
  .RiskDelta          // int
  .DifficultyMultiplier   // double (1.0 – 1.75)
  .RewardPowerMultiplier  // double (= DifficultyMultiplier)
  .RiskBand           // RiskBand enum
```

The DTO equivalent (`CombatScalingDto`) is included in the `RewardOfferDto` sent to
the API consumer.

---

## Future work

* Apply `DifficultyMultiplier` to enemy `MaxHealth` and `Attack` inside
  `CombatInstanceFactory` when creating combat encounters from nodes.
* Feed `RewardPowerMultiplier` into the item-generation pipeline so generated
  equipment carries proportionally stronger stat rolls or affixes.
