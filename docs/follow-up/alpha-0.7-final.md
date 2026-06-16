# alpha-0.7 — Backend Milestone Report

Version: `alpha-0.7.FINAL-rc1`
Status: Release candidate — pending Catalog and Player build validation
Date: 2026-06-16

---

## Objectif du cycle alpha-0.7

Migrer le backend Game Engine d'une architecture partiellement hardcodée vers un pipeline data-driven complet, en utilisant Catalog comme source canonique des définitions et Game Engine comme propriétaire des snapshots et du runtime state.

---

## PRs incluses

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
| alpha-0.7.11 | Markov engine foundation | ✅ (pré-existante) |
| alpha-0.7.12 | Apply Markov weights to catalog selection | ✅ |
| alpha-0.7.13 | Palace adaptive influence projections | ✅ |
| alpha-0.7.14 | Stabilize alpha 0.7 data-driven pipeline | ✅ |

### Note on alpha-0.7.11

`alpha-0.7.11` did not introduce new code because the Markov foundation already existed before the final alpha-0.7 consolidation. The existing implementation includes `MarkovState`, `MarkovStateDistribution`, `MarkovTransitionRow`, `MarkovTransitionMatrix`, `MarkovTransitionResolver`, and `DeterministicMarkovSampler`. It was validated through 29 existing Markov mathematical tests covering matrix row validation (sum = 1), deterministic seeded sampling, transition distribution checks, and matrix versioning.

---

## Validation multi-services

| Service | Restore | Build | Tests | Notes |
|---|---:|---:|---:|---|
| Game Engine | ✅ | ✅ | ✅ | 1013 unit tests + 75 integration tests = 1088 total |
| Catalog | — | ❌ | — | File lock: Leds.Catalog.Api (PID 13600) locks DLLs. No compilation errors. |
| Player | — | ❌ | — | File lock: Leds.Player.Api (PID 19876) locks DLLs. No compilation errors. |

### Migration script

| Script | Status | Notes |
|---|---:|---|
| scripts/dev/apply-migrations.ps1 | ⚠️ | Game Engine migrations applied. Player migration fails (pre-existing, unrelated to alpha-0.7). |

### Blocking validation issue

Catalog and Player fail `dotnet build` because running API processes lock the output DLLs (MSB3027/MSB3021). These are **not compilation errors** — no `error CS` diagnostics are produced. The failures are environmental and resolve when the API processes are stopped.

To fully validate, stop the running services and re-run:

```powershell
# Stop running APIs, then:
dotnet restore services/catalog/Leds.Catalog.slnx
dotnet build services/catalog/Leds.Catalog.slnx
dotnet test services/catalog/Leds.Catalog.slnx

dotnet restore services/player/Leds.Player.slnx
dotnet build services/player/Leds.Player.slnx
dotnet test services/player/Leds.Player.slnx
```

Until this validation completes, alpha-0.7 remains `FINAL-rc1`.

---

## Pipeline data-driven obtenu

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

---

## Catalog definitions

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

---

## Player permanent progression

- Player possède la progression permanente
- Game Engine crée des snapshots depuis Player au démarrage de run
- Pas de cross-service FK

---

## Game Engine run snapshots

| Table | Status | PR |
|---|---|---|
| `run_player_snapshots` | Created | alpha-0.7.3 |
| `run_character_snapshots` | Created | alpha-0.7.3 |
| `run_character_stat_snapshots` | Created | alpha-0.7.3 |
| `run_character_skill_snapshots` | Created | alpha-0.7.3 |

---

## Combat snapshots/runtime state

| Table | Status | PR |
|---|---|---|
| `run_combatant_base_stat_snapshots` | Created | alpha-0.7.4 |
| `run_combatant_runtime_states` | Created | alpha-0.7.4 |
| `run_combat_actions` | Created | alpha-0.7.5 |
| `run_combat_metrics` | Created | alpha-0.7.5 |

---

## Effects/modifiers

- `RuntimeEffectResolver` résout les effets depuis `EffectSet`
- `RunModifier` avec `SourceType`, `SourceKey`, `ValueMode`, `StackPolicy`, `Duration`
- Types supportés : `AddStartingGuard`, `ModifyDifficultyMultiplier`, `ModifyRewardPowerMultiplier`, `ModifyAttackPower`, `ModifyDefense`, `ModifySpeed`, `ModifyInitiative`, `ModifyRecovery`

---

## Rewards/items

| Table | Status | PR |
|---|---|---|
| `run_reward_offers` | Created | alpha-0.7.7 |
| `run_reward_options` | Created | alpha-0.7.7 |
| `run_items` | Enriched (12 new columns) | alpha-0.7.7 |

- `RewardOffer` + `RewardOption` persistés via `EfRewardOfferRepository`
- `SelectRewardCommandHandler` enrichit les items via `ICatalogItemDefinitionProvider`

---

## Laws/curses

| Table | Status | PR |
|---|---|---|
| `run_active_palace_laws` | Enriched (6 new columns) | alpha-0.7.8 |
| `run_active_curses` | Created | alpha-0.7.8 |
| `run_modifiers` | Already existed, aligned | alpha-0.7.6 |

- `ActivePalaceLaw` enrichi avec `DisplayName`, `Description`, `Duration`, `AppliedAtUtc`, `ExpiresAtRoomId`, `ConsumedAtUtc`
- `ActiveCurse` enrichi avec `Id`, `CurseDefinitionKey`, `Severity`, `Duration`, `EffectSetKey`, `ConsumedAtUtc`
- `CurseEventChoiceResolver` gère accept/reject avec RunModifier

---

## Enemies/encounters

- `ICatalogEnemyDefinitionProvider` + `ICatalogRoomEnemyPoolProvider`
- `DeterministicEncounterEnemySelector` sélectionne les ennemis par seed
- `CombatEncounterDraftGenerator` utilise les abstractions Catalog
- `EncounterCompositionPolicy` filtre par budget/archetype

---

## Deterministic selection

| Table | Status | PR |
|---|---|---|
| `run_selection_decisions` | Created | alpha-0.7.10 |

- `SelectionContext`, `SelectionCandidate`, `SelectionDecision` (domain models)
- `DeterministicWeightedSelector` — sélection pondérée déterministe par seed
- `MarkovSelectionInfluence` — influence Markov sur les poids (alpha-0.7.12)
- `IEncounterEnemySelector` utilise le sélecteur générique

---

## Markov foundation

Existed before alpha-0.7, validated through existing tests :
- `MarkovState`, `MarkovStateDistribution`, `MarkovTransitionRow`, `MarkovTransitionMatrix`
- `MarkovTransitionResolver` + `DeterministicMarkovSampler`
- Validation des lignes (somme = 1), versioning (`Key`, `Version`)
- 29 tests mathématiques ✅

---

## Palace qualitative indicators

| Table | Status | PR |
|---|---|---|
| `run_adaptive_influences` | Created | alpha-0.7.13 |
| `run_palace_indicator_snapshots` | Created | alpha-0.7.13 |

- `AdaptiveInfluence` — projections runtime des biais/comportements
- `PalaceIndicator` — indicateurs narratifs frontend-safe
- Aucune matrice Markov exposée

---

## Gameplay impact

alpha-0.7 does not primarily add visible frontend features. It makes the backend data-driven :
- runs are snapshot-based ;
- combatants separate immutable base stats from mutable runtime state ;
- rewards/items/laws/curses/enemies use Catalog-driven definitions ;
- combat actions and metrics are server-authoritative ;
- selection decisions are deterministic and persistable ;
- Markov influence is introduced without exposing internal matrices.

---

## Legacy remaining / temporary fallbacks

| Fallback | Raison | Acceptable | Cible |
|---|---|---|---|
| Providers InMemory Catalog | Intégration HTTP pas encore complète | Oui (transition) | alpha-0.8/0.9 |
| Markov influence enemy-only | Adaptation encore partielle | Oui (progressif) | alpha-0.8.1/0.8.2 |
| Palace indicators non connectés | Backend prêt, UI non branchée | Oui (séparation frontend) | web-client future PR |
| ATB runtime non implémenté | Combat reste non-ATB | Oui (hors scope alpha-0.7) | alpha-0.8.5 |
| Colonnes legacy conservées | Compatibilité backward | Oui (migration additive) | PR dédiée si suppression |

### Temporary providers

The following providers still have InMemory implementations used as transitional adapters :

| Provider | InMemory | HTTP | Replacement target |
|---|---|---|---|
| `ICatalogContentGateway` | ✅ | ✅ | Already dual-mode |
| `ICatalogEnemyDefinitionProvider` | ✅ | ❌ | alpha-0.8/0.9 |
| `ICatalogRoomEnemyPoolProvider` | ✅ | ❌ | alpha-0.8/0.9 |
| `ICatalogRewardTemplateProvider` | ✅ | ❌ | alpha-0.8/0.9 |
| `ICatalogItemDefinitionProvider` | ✅ | ❌ | alpha-0.8/0.9 |
| `ICatalogPalaceLawDefinitionProvider` | ✅ | ❌ | alpha-0.8/0.9 |
| `ICatalogCurseDefinitionProvider` | ✅ | ❌ | alpha-0.8/0.9 |
| `ICatalogEffectSetProvider` | ✅ | ❌ | alpha-0.8/0.9 |

Les providers InMemory ne doivent pas être considérés comme architecture finale.

---

## Limitations restantes

| Limitation | Impact | Target |
|---|---|---|
| Providers InMemory temporaires | Intégration Catalog pas encore entièrement HTTP | alpha-0.8/0.9 |
| Markov influence enemy-only | Adaptation encore partielle | alpha-0.8.1/0.8.2 |
| Palace indicators non connectés au frontend | Backend prêt mais UI non branchée | web-client future PR |
| ATB non implémenté | Combat reste non-ATB | alpha-0.8.5 |
| `apply-migrations.ps1` Player fail | Player migration blocked (pre-existing) | Player fix PR |

---

## Definition of Done for alpha-0.7.FINAL

alpha-0.7.FINAL can be considered closed only if :

- [x] Game Engine restore/build/test pass
- [ ] Catalog restore/build/test pass (blocked by file lock, no compilation errors)
- [ ] Player restore/build/test pass (blocked by file lock, no compilation errors)
- [x] Game Engine EF migrations apply without pending model changes
- [x] No Markov internals are exposed through API/frontend DTOs
- [x] Existing gameplay endpoints remain compatible
- [x] Remaining InMemory providers are explicitly documented as temporary
- [x] Known limitations are listed with target follow-up milestones

---

## Roadmap alpha-0.8

- alpha-0.8.1 : Markov adaptatif complet sur rooms.
- alpha-0.8.2 : Markov adaptatif sur rewards/laws/curses.
- alpha-0.8.3 : Palace pressure system complet.
- alpha-0.8.4 : Frontend indicators integration.
- alpha-0.8.5 : ATB runtime foundation.
- alpha-0.8.6 : Enemy AI overhaul.
- alpha-0.8.7 : Multi-enemy encounter balancing.
- alpha-0.8.8 : Interlude / Him'Lit / narrative long loop preparation.

## Roadmap alpha-0.9

- API Gateway hardening.
- Security review.
- Observability.
- Staging deployment.
- External alpha readiness.
- Documentation and release hardening.

---

## Tables Game Engine created or significantly changed in alpha-0.7

| Table | Status | PR |
|---|---|---|
| `run_player_snapshots` | Created | alpha-0.7.3 |
| `run_character_snapshots` | Created | alpha-0.7.3 |
| `run_character_stat_snapshots` | Created | alpha-0.7.3 |
| `run_character_skill_snapshots` | Created | alpha-0.7.3 |
| `run_combatant_base_stat_snapshots` | Created | alpha-0.7.4 |
| `run_combatant_runtime_states` | Created | alpha-0.7.4 |
| `run_combat_actions` | Created | alpha-0.7.5 |
| `run_combat_metrics` | Created | alpha-0.7.5 |
| `run_modifiers` | Enriched | alpha-0.7.6 |
| `run_reward_offers` | Created | alpha-0.7.7 |
| `run_reward_options` | Created | alpha-0.7.7 |
| `run_items` | Enriched (12 columns) | alpha-0.7.7 |
| `run_active_palace_laws` | Enriched (6 columns) | alpha-0.7.8 |
| `run_active_curses` | Created | alpha-0.7.8 |
| `run_selection_decisions` | Created | alpha-0.7.10 |
| `run_adaptive_influences` | Created | alpha-0.7.13 |
| `run_palace_indicator_snapshots` | Created | alpha-0.7.13 |

---

## Tests et validation

**Game Engine :**
- 1013 unit tests ✅
- 75 integration tests ✅
- Total : 1088 tests

**Migrations EF Game Engine :**
- 14 migrations appliquées avec succès
- Aucune migration destructive

**Markov foundation :**
- 29 tests mathématiques ✅

---

## Fichiers modifiés (documentation uniquement)

- `docs/follow-up/alpha-0.7-final.md` — ce document
