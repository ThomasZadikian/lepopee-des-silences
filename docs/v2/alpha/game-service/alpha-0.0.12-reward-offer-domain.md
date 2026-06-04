# L'épopée des silences — Suivi technique alpha-0.0.12

## PR — Reward Offer Domain + Factory + Repository + Tests

**Service concerné :** `services/game-engine`
**Version concernée :** `alpha-0.0.12`
**Type de PR :** feat / domaine récompense runtime
**Statut final :** validé si build et tests passent
**Diffusion :** confidentiel projet

---

## 1. Contexte

Le jalon `alpha-0.0.10` a introduit la création d'une `RewardOffer` à la fin du combat via la `RewardOfferFactory`. Le jalon `alpha-0.0.12` consolide le domaine RewardOffer en complétant son implémentation, en ajoutant le repository, les DTOs applicatifs, l'endpoint API et les tests unitaires.

---

## 2. Problème initial

Avant cette PR, le domaine RewardOffer était partiellement implémenté :

```text
- RewardOffer, RewardChoice, RewardSource existaient dans le domaine ;
- RewardOfferFactory créait une offre basique ;
- pas de repository dédié ;
- pas de DTO applicatif ;
- pas d'endpoint API pour consulter l'offre en attente ;
- pas de tests unitaires ;
- pas de sérialisation JSON des offres.
```

---

## 3. Objectif de la PR

Compléter le domaine RewardOffer et l'exposer via l'API :

```text
- RewardOffer full domain avec invariants ;
- RewardChoice avec héritage typé ;
- RewardSource (Combat, RoomBoss, etc.) ;
- RewardOfferFactory connectée aux templates Catalog ;
- IRewardOfferRepository + InMemoryRewardOfferRepository ;
- RewardOfferDto applicatif ;
- endpoint GET /runs/{id}/rewards/pending ;
- tests unitaires du domaine et de la factory.
```

---

## 4. Éléments ajoutés

### 4.1 Domaine

```text
src/Leds.GameEngine.Domain/Rewards/
├── RewardOffer.cs
├── RewardChoice.cs
├── RewardSource.cs
├── RewardOfferId.cs
├── RewardChoiceId.cs
└── IRewardOfferRepository.cs (port)
```

### 4.2 Infrastructure

```text
src/Leds.GameEngine.Infrastructure/Rewards/
├── InMemoryRewardOfferRepository.cs
└── RewardOfferFactory.cs
```

### 4.3 Application

```text
src/Leds.GameEngine.Application/Rewards/
├── Ports/IRewardOfferRepository.cs (référencement)
├── RewardOfferFactory/
│   ├── RewardOfferFactory.cs
│   └── IRewardOfferFactory.cs
├── Dtos/
│   ├── RewardOfferDto.cs
│   └── RewardChoiceDto.cs
└── Controllers/RewardsController.cs
```

### 4.4 Tests

```text
tests/Leds.GameEngine.UnitTests/Rewards/
└── RewardOfferFactoryTests.cs (9 tests)
```

---

## 5. Détail du domaine

### 5.1 RewardOffer

```text
- Id : RewardOfferId (ValueObject)
- Source : RewardSource (Combat, RoomBoss, Exploration, etc.)
- RiskLevel : int (0-100)
- Choices : IReadOnlyList<RewardChoice>
- SelectedChoiceId : RewardOfferId? (null tant que non sélectionné)
- CreatedAt : DateTime
- SelectChoice(choiceId) → valide et marque le choix
```

### 5.2 RewardChoice

Classe de base abstraite avec héritage typé :

```text
RewardChoice (abstract)
├── Id : RewardChoiceId
└── Label : string

ItemRewardChoice
├── ItemTemplateKey : string
└── ItemTemplateVersion : string

StatRewardChoice
├── StatType : string
└── Value : int

HealRewardChoice
├── HealAmount : int
└── HealType : string

CurrencyRewardChoice
├── CurrencyType : string
└── Amount : int
```

### 5.3 RewardSource

```csharp
public enum RewardSource
{
    Combat = 0,
    RoomBoss = 1,
    Elite = 2,
    Exploration = 3,
    Npc = 4,
    Event = 5
}
```

---

## 6. RewardOfferFactory

```text
CreateCombatRewardOffer(source, riskLevel)
→ génère 2-3 choix aléatoires parmi les templates disponibles
→ utilise ICatalogContentGateway pour charger les templates
→ crée ItemRewardChoice, StatRewardChoice ou HealRewardChoice

Implémentation actuelle :
→ génération déterministe basée sur la seed de la run
→ 2 choix garantis
→ chaque choix est un ItemRewardChoice avec un template ennemi shadow
```

---

## 7. Endpoint API

```text
GET /api/v2/runs/{runId}/rewards/pending
→ retourne RewardOfferDto si une offre est en attente
→ 404 si run non trouvée
→ 204 (NoContent) si aucune offre en attente
```

Ajouté dans `RewardsController.cs`.

---

## 8. Tests unitaires (9 tests)

```text
Create_ShouldReturnFailure_WhenSourceIsInvalid
Create_ShouldReturnFailure_WhenRiskLevelIsOutOfRange
Create_ShouldReturnSuccess_WhenParametersAreValid
Create_ShouldReturnOfferWithAtLeastTwoChoices
Create_ShouldReturnOfferWithOnlyValidChoiceTypes
Create_ShouldReturnDeterministicOfferForSameSeed
Create_ShouldReturnDifferentOfferForDifferentSeed
Create_ShouldReturnSuccess_ForCombatSource
Create_ShouldReturnSuccess_ForRoomBossSource
```

---

## 9. Critères de validation

```text
- RewardOffer respecte ses invariants domaine ;
- RewardOfferFactory crée des offres valides et déterministes ;
- InMemoryRewardOfferRepository stocke et retourne les offres ;
- endpoint GET /rewards/pending fonctionne ;
- 9 tests unitaires RewardOfferFactory passent ;
- tous les tests existants continuent de passer.
```
