# alpha-0.0.14 — Room progression loop (locking rules, invariants)

## Goal
Prevent room progression while the run is in a state that cannot advance (active combat, pending reward offer, unresolved event choice).

## Changes

### Application — `ProgressRunCommandHandler`
- Added guard: `run.HasActiveCombat` → throws `DomainException` before `ProgressCurrentRoom()`.
- Added guard: `run.HasPendingRewardOffer` → throws `DomainException` before `ProgressCurrentRoom()`.
- Existing guard (choice requirement) was already present.

### Tests — `ProgressRunEndpointTests` (integration, 2 new tests)
1. **`ProgressRun_ShouldReturnBadRequest_WhenCombatIsActive`** — chooses a combat node, resolves (creates combat), attempts progress → 400.
2. **`ProgressRun_ShouldReturnBadRequest_WhenRewardIsPending`** — chooses a combat node, resolves + completes combat (without selecting reward), attempts progress → 400.

### Fixed flaky test — `ResolveCurrentEventEndpointTests`
- `ResolveCurrentEvent_ShouldStartCombat_WhenEventIsCombat` now filters for `EventTypes.First() == "Combat"` instead of `Contains("Combat")`; the resolver uses the primary (first) event type.

## Test results
- 224 unit tests pass (no change)
- 28 integration tests pass (+2 new, 1 fixed flaky)
- 5 consecutive full runs confirmed stable
