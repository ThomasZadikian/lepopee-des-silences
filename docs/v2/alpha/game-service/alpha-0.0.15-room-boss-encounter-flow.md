# alpha-0.0.15 — Room boss encounter flow

## Goal
Make room boss encounters go through the combat pipeline (instead of resolving immediately as a non-combat event), so the player fights the boss, receives a boss-tagged reward, and the room completes.

## Changes

### New content types
- **`ResolvedRoomBossEventContent`** — new sealed record, same shape as `ResolvedCombatEventContent` but with `ResolvedEventContentKind.Boss`.
- **`ResolvedEventContentKind.Boss = 10`** — added to the enum.

### Infrastructure — `RoomBossEventContentResolutionStrategy`
- New strategy supporting `NodeEventType.RoomBoss`.
- Default templates: `event-boss-threshold-guardian-v1`, `boss-threshold-guardian-v1`.
- Registered in DI.

### EventContentResolver
- Removed `NodeEventType.RoomBoss` from `UnsupportedStandardPipelineEventTypes` so boss events flow through the standard pipeline.

### Catalog — `InMemoryCatalogContentGateway`
Added templates:
| Type | Key | Details |
|------|-----|---------|
| Enemy | `boss-threshold-guardian-v1` | HP 50, ATK 10, DEF 6, SPD 8, Affinity Void, skill `skill-boss-void-slam-v1` |
| Skill | `skill-boss-void-slam-v1` | Power 14, type Void |
| Event | `event-boss-threshold-guardian-v1` | Type RoomBoss, tags `["test", "boss"]` |

### Application — `ResolveCurrentEventCommandHandler`
- Added `RoomBossEncounterStarted` to the `isCombat` check alongside `CombatStarted` and `EliteEncounterStarted`.
- Added `ResolvedRoomBossEventContent` to the content-type switch that extracts `EnemyTemplateKey`.

### Bonus detection (already wired)
- `SubmitCombatActionCommandHandler` at line 118 checks `TemplateKey.Contains("boss")` → the key `boss-threshold-guardian-v1` triggers `RewardSource.RoomBoss`.

### Tests
- `EventContentResolverTests.ResolveAsync_ShouldRejectRoomBossContent` → **changed to** `ResolveAsync_ShouldResolveRoomBossContent` (now expects success with boss content).
- `EventContentResolverTests` setup registers `RoomBossEventContentResolutionStrategy`.
- `RoomBossProgressionEndpointTests` works unchanged — the boss node now creates combat, flows through `ResolveAndHandleCombatAsync`, and the room completes as before.

## Test results
- 224 unit tests pass (1 updated, 223 unchanged)
- 28 integration tests pass (all unchanged)
- 3 consecutive full runs confirmed stable
