# alpha-0.7 — Final Backend Milestone Report

Version: `alpha-0.7.FINAL`
Date: 2026-06-16

## 1. Objectif du cycle alpha-0.7

Migrer le backend Game Engine d'une architecture partiellement hardcodée vers un pipeline data-driven complet, en utilisant Catalog comme source canonique des définitions et Game Engine comme propriétaire des snapshots et du runtime state.

## 2. PRs incluses

| PR | Nom | Status |
|---|---|---|
| alpha-0.7.1 | Catalog relationnel aligné data-model-0.1 | ✅ |
| alpha-0.7.2 | Player permanent progression alignée + fix EF Game Engine | ✅ |
| alpha-0.7.3 | Run snapshots depuis Player | ✅ |
| alpha-0.7.4 | Combatant base stat snapshots + runtime states | ✅ |
| alpha-0.7.5 | Combat actions + official combat metrics | ✅ |
| alpha-0.7.6 | RuntimeEffectResolver + RunModifier officiel | ✅ |
| alpha-0.7.7 | Rewards/items via Catalog EffectSet | ✅ |
| alpha-0.7.8 | Laws/curses via official effect model | ✅ |
| alpha-0.7.9 | Encounters/enemies via Catalog definitions | ✅ |
| alpha-0.7.10 | Deterministic selection context | ✅ |
| alpha-0.7.11 | Markov engine foundation | ✅ (déjà existante) |
| alpha-0.7.12 | Apply Markov weights to catalog selection | ✅ |
| alpha-0.7.13 | Palace adaptive influence projections | ✅ |
| alpha-0.7.14 | Stabilize alpha 0.7 data-driven pipeline | ✅ |

## 3. Modèle data-driven obtenu

Le pipeline data-driven est maintenant complet :

```
Catalog Definitions (source canonique)
    ↓ (snapshots via abstractions)
Game Engine Runtime State
    ↓ (deterministic selection + Markov influence)
Combat / Rewards / Laws / Curses / Enemies
    ↓ (RuntimeEffectResolver)
RunModifiers / ActivePalaceLaw / ActiveCurse
    ↓ (persistence)
PostgreSQL (tables de snapshots)
```

## 4. Catalog definitions

**Entities Catalog :**
- `EnemyDefinition` + `EnemyStatBlock` + `EnemySkillLinks`
- `SkillDefinition`
- `ItemDefinition`
- `PalaceLawDefinition`
- `CurseDefinition`
- `EffectSet` + `EffectDefinition`
- `RewardTemplate` + `RewardTemplateOption`
- `RoomDefinition` + `RoomEnemyPool`
- `EventTemplate`

**Abstractions Game Engine :**
- `ICatalogContentGateway` (HTTP + InMemory)
- `ICatalogEnemyDefinitionProvider`
- `ICatalogRoomEnemyPoolProvider`
- `ICatalogRewardTemplateProvider`
- `ICatalogItemDefinitionProvider`
- `ICatalogPalaceLawDefinitionProvider`
- `ICatalogCurseDefinitionProvider`
- `ICatalogEffectSetProvider`

## 5. Player permanent progression

- Player possède la progression permanente
- Game Engine crée des snapshots depuis Player au démarrage de run
- Pas de cross-service FK

## 6. Game Engine run snapshots

Tables créées :
- `run_player_snapshots`
- `run_character_snapshots`
- `run_character_stat_snapshots`
- `run_character_skill_snapshots`

## 7. Combat snapshots/runtime state

Tables créées :
- `run_combatant_base_stat_snapshots`
- `run_combatant_runtime_states`
- `run_combat_actions`
- `run_combat_metrics`

## 8. Combat metrics

- Métriques officielles persistées par combat action
- Tracking des dégâts, soins, guards, skills utilisés

## 9. Effects/modifiers

- `RuntimeEffectResolver` résout les effets depuis `EffectSet`
- `RunModifier` avec `SourceType`, `SourceKey`, `ValueMode`, `StackPolicy`, `Duration`
- Types supportés : `AddStartingGuard`, `ModifyDifficultyMultiplier`, `ModifyRewardPowerMultiplier`, etc.

## 10. Rewards/items

- `RewardOffer` + `RewardOption` persistés via `EfRewardOfferRepository`
- `RunItem` enrichi avec 12 champs snapshot Catalog
- `SelectRewardCommandHandler` enrichit les items via `ICatalogItemDefinitionProvider`

## 11. Laws/curses

- `ActivePalaceLaw` enrichi avec `DisplayName`, `Description`, `Duration`, `AppliedAtUtc`, `ExpiresAtRoomId`, `ConsumedAtUtc`
- `ActiveCurse` enrichi avec `Id`, `CurseDefinitionKey`, `Severity`, `Duration`, `EffectSetKey`, `ConsumedAtUtc`
- Table `run_active_curses` créée
- `CurseEventChoiceResolver` gère accept/reject avec RunModifier

## 12. Enemies/encounters

- `ICatalogEnemyDefinitionProvider` + `ICatalogRoomEnemyPoolProvider`
- `DeterministicEncounterEnemySelector` sélectionne les ennemis par seed
- `CombatEncounterDraftGenerator` utilise les abstractions Catalog

## 13. Deterministic selection

- `SelectionContext`, `SelectionCandidate`, `SelectionDecision` (domain models)
- `DeterministicWeightedSelector` — sélection pondérée déterministe par seed
- Table `run_selection_decisions` persiste les décisions
- `IEncounterEnemySelector` utilise le sélecteur générique

## 14. Markov foundation

Déjà existante avant alpha-0.7 :
- `MarkovState`, `MarkovStateDistribution`, `MarkovTransitionRow`, `MarkovTransitionMatrix`
- `MarkovTransitionResolver` + `DeterministicMarkovSampler`
- Validation des lignes (somme = 1)
- Versioning (`Key`, `Version`)
- 29 tests mathématiques

## 15. Palace qualitative indicators

- `AdaptiveInfluence` — projections runtime des biais/comportements
- `PalaceIndicator` — indicateurs narratifs frontend-safe
- Tables `run_adaptive_influences`, `run_palace_indicator_snapshots`
- Aucune matrice exposée

## 16. Tests et validation

**Game Engine :**
- 1013 unit tests ✅
- 75 integration tests ✅
- Total : 1088 tests

**Migrations EF :**
- 14 migrations appliquées avec succès
- Aucune migration destructive

**Build :**
- `dotnet build services/game-engine/Leds.GameEngine.slnx` ✅
- `dotnet test services/game-engine/Leds.GameEngine.slnx` ✅

## 17. Limitations restantes

- Providers InMemory sont temporaires (remplacement HTTP à venir)
- Markov influence uniquement sur enemy selection (pas encore rooms/rewards/laws)
- Palace indicators non connectés au frontend
- Pas d'ATB runtime
- Pas de Markov adaptatif complet
- Player Service a des problèmes de build pré-existants (hors scope)

## 18. Roadmap alpha-0.8

- alpha-0.8.1 : Markov adaptatif complet (rooms, rewards, laws, curses)
- alpha-0.8.2 : Palace pressure system complet
- alpha-0.8.3 : Frontend indicators integration
- alpha-0.8.4 : ATB runtime foundation
- alpha-0.8.5 : Enemy AI overhaul
- alpha-0.8.6 : Multi-enemy encounter balancing
- alpha-0.8.7 : Permanent curse/unlock system
- alpha-0.8.8 : Security, gateway, externalization

## 19. Tables Game Engine créées dans alpha-0.7

| Table | PR |
|---|---|
| `run_player_snapshots` | alpha-0.7.3 |
| `run_character_snapshots` | alpha-0.7.3 |
| `run_character_stat_snapshots` | alpha-0.7.3 |
| `run_character_skill_snapshots` | alpha-0.7.3 |
| `run_combatant_base_stat_snapshots` | alpha-0.7.4 |
| `run_combatant_runtime_states` | alpha-0.7.4 |
| `run_combat_actions` | alpha-0.7.5 |
| `run_combat_metrics` | alpha-0.7.5 |
| `run_modifiers` | alpha-0.7.6 |
| `run_reward_offers` | alpha-0.7.7 |
| `run_reward_options` | alpha-0.7.7 |
| `run_active_curses` | alpha-0.7.8 |
| `run_selection_decisions` | alpha-0.7.10 |
| `run_adaptive_influences` | alpha-0.7.13 |
| `run_palace_indicator_snapshots` | alpha-0.7.13 |
