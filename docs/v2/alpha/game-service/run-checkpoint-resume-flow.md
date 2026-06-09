# Run Checkpoint & Resume Flow

**PR:** 0.2.2  
**Status:** Implemented

---

## Overview

This PR extends the Save & Exit flow from PR 0.2.1 with two new capabilities:

1. **ExitMidRoom** — Quit during an active room, rolling back all progress to the room entry snapshot
2. **Resume** — Restore a suspended run to its pre-suspend state so play can continue

The checkpoint model is lightweight: instead of snapshotting exact node/combat state, the system stores a `RunSnapshot` at room entry (HP, stats, memory fragments, palace laws). On exit-mid-room, it rolls back to that snapshot and resets the room map. On resume, it restores the pre-suspend status and recreates the snapshot so the player can exit again.

---

## ExitMidRoom

### When

The player clicks "⟳ Quitter la salle" (visible on `RunDangerActions.vue` when the run is Active and not in Interlude or RoomCleared).

### Behaviour

```
Active + no combat + no pending reward
  → Rollback HP / stats / memory fragments / palace laws to room-entry snapshot
  → Reset room map (all nodes to initial state, CurrentNodeDepth = 0)
  → Status = Suspended
  → SavedAt = now
  → Return to title screen
```

### Domain: `Run.ExitMidRoom(DateTimeOffset savedAt)`

Guards (in order):
1. Run is not closed or suspended
2. Status is `Active`
3. No active combat
4. No pending reward offer
5. `_roomSnapshot` is not null

Effect:
- Rollback all mutable stats to snapshot values
- Clear and restore memory fragments and palace laws from snapshot
- `CurrentRoom.ResetProgress()` — resets node states and depth
- `_preSuspendStatus = RunStatus.Active`
- `Status = RunStatus.Suspended`
- `SavedAt = savedAt`
- `_roomSnapshot = null`

### Application: `ExitMidRoomCommandHandler`

Route: `POST /api/v2/runs/{runId}/exit-mid-room`

Loads run by ID, calls `ExitMidRoom(clock.UtcNow)`, persists via `IRunRepository.UpdateAsync`.

### Frontend: `runStore.exitMidRoom()`

1. Calls `POST /api/v2/runs/{runId}/exit-mid-room`
2. Stores run ID in `localStorage` key `resumableRunId`
3. Navigates to title screen (`router.push('/')`)

---

## SaveAndExit (from RoomCleared / Interlude)

### Behaviour

```
RoomResolved or Interlude + no combat + no reward
  → Status = Suspended
  → SavedAt = now
  → _preSuspendStatus = RoomResolved or Interlude
  → Return to title screen
```

### Domain: `Run.SaveAndExit(DateTimeOffset savedAt)`

Guards (in order):
1. Run is not closed or already suspended
2. Status is `RoomResolved` or `Interlude`
3. No active combat
4. No pending reward offer

Effect:
- `_preSuspendStatus = Status` (preserves current status)
- `Status = RunStatus.Suspended`
- `SavedAt = savedAt`

Route: `POST /api/v2/runs/{runId}/save-and-exit`

---

## Resume

### Behaviour

```
Suspended + _preSuspendStatus is not null
  → Status = _preSuspendStatus
  → SavedAt = null
  → _preSuspendStatus = null
  → If restored to Active: recreate _roomSnapshot (so player can exit again)
  → Player continues from where they left off
```

### Domain: `Run.Resume()`

Guards:
1. Status is `Suspended`
2. `_preSuspendStatus` is not null

Effect:
- `Status = _preSuspendStatus.Value`
- `SavedAt = null`
- `_preSuspendStatus = null`
- If restored to `Active`: `_roomSnapshot = CreateSnapshot()`

### Resume by status

| Pre-suspend status | Restored status | Player sees | Can exit again? |
|---|---|---|---|
| `Active` | `Active` | Room map (reset to start of room) | Yes (snapshot recreated) |
| `RoomResolved` | `RoomResolved` | "Salle terminée" panel → Enter Interlude | No (not Active) |
| `Interlude` | `Interlude` | Interlude hub | No (not Active) |

### Application: `ResumeRunCommandHandler`

Route: `POST /api/v2/runs/{runId}/resume`

Loads run by ID, calls `run.Resume()`, persists via `IRunRepository.UpdateAsync`.

### Frontend: `runStore.loadRun()`

On app load, the store checks `localStorage` for a `resumableRunId`. If found, it calls `POST /api/v2/runs/{runId}/resume` then refreshes the run by ID. If the resume fails (run abandoned or not found), the localStorage key is cleared.

---

## Abandon

Guards (handler-level):
- Only allowed from safe points (`RoomResolved` or `Interlude`)

Effect:
- `Status = Abandoned`
- `EndedAt = now`
- Not resumable (`Suspended` runs only)
- `CurrentRoomIndex` unchanged

Route: `POST /api/v2/runs/{runId}/abandon`

---

## Checkpoint Model

The `RunSnapshot` is an in-memory record captured at room entry (`StartNew` and `MoveToNextRoom`):

```csharp
private sealed record RunSnapshot(
    int CurrentHp,
    int Attack,
    int Defense,
    int Speed,
    string[] MemoryFragments,
    ActivePalaceLaw[] ActivePalaceLaws);
```

**What is preserved:**
- Player stats (HP, Attack, Defense, Speed)
- Memory fragments collected so far
- Active palace laws

**What is NOT preserved (intentional):**
- Exact node positions / selected nodes
- Combat state
- Event choices
- Room map layout (the room is reset to initial state)

**Why this model?**
- Simple implementation — no serialization of complex node/combat state
- The room is a "attempt" — the player re-enters from the beginning
- Future persistent storage can save the snapshot alongside the run

---

## RunDto Resume Fields

| Field | Type | Description |
|---|---|---|
| `CanResume` | `bool` | `true` when `Status == Suspended` |
| `SavedAt` | `DateTimeOffset?` | Set by `SaveAndExit` / `ExitMidRoom`; cleared on resume |

---

## Rules Matrix

| Rule | Check |
|---|---|
| ExitMidRoom from Active → Suspended | Resets room, sets SavedAt, preserves index |
| ExitMidRoom fails if combat active | Domain guard |
| ExitMidRoom fails if reward pending | Domain guard |
| ExitMidRoom fails if not Active | Domain guard |
| SaveAndExit from RoomResolved → Suspended | Preserves index |
| SaveAndExit from Interlude → Suspended | Preserves index |
| Resume → Active restores room state | Room is reset, no selected/resolved nodes |
| Resume → RoomResolved allows EnterInterlude | Status restored correctly |
| Resume → Interlude shows interlude hub | Status restored correctly |
| Resume fails if not Suspended | Domain guard |
| Resume fails if Abandoned/Completed/Failed | Domain guard |
| Abandon → not resumable | Status is Abandoned, not Suspended |
| Abandon → index unchanged | CurrentRoomIndex preserved |
| Resume → can exit again | Snapshot recreated for Active runs |
| Game actions blocked when Suspended | `EnsureActive()` guard |

---

## State Transitions

```
Active ───(choose node, resolve, progress)──→ RoomResolved
Active ───(ExitMidRoom)──────────────────────→ Suspended
RoomResolved ───(SaveAndExit)────────────────→ Suspended
RoomResolved ───(EnterInterlude)─────────────→ Interlude
Interlude ───(SaveAndExit)───────────────────→ Suspended
Interlude ───(EnterNextRoom)─────────────────→ Active (next room)
Suspended ───(Resume)────────────────────────→ Active | RoomResolved | Interlude
```

---

## Files Changed

**Domain**
- `Run.cs.cs` — `_roomSnapshot`, `_preSuspendStatus`, `RunSnapshot`, `ExitMidRoom()`, `Resume()`, `CreateSnapshot()`
- `Room.cs` — `ResetProgress()`
- `MapNode.cs` — `ResetToInitial()`

**Application**
- `Runs/ExitMidRoom/` — new command / handler / response
- `Runs/ResumeRun/` — new command / handler / response

**API**
- `RunsController.cs` — added `POST .../exit-mid-room` and `POST .../resume`

**Frontend**
- `runStore.ts` — `exitMidRoom()`, auto-resume in `loadRun()`
- `runApi.ts` — `exitMidRoom()`, `resumeRun()`
- `RunDangerActions.vue` — "⟳ Quitter la salle" button

**Tests**
- `Runs/ExitMidRoom/ExitMidRoomTests.cs` — 18 domain tests
- `Runs/ExitMidRoom/ExitMidRoomCommandHandlerTests.cs` — 5 handler tests
- `Runs/ResumeRun/ResumeRunTests.cs` — 21 domain tests
- `Runs/ResumeRun/ResumeRunCommandHandlerTests.cs` — 9 handler tests
- `Runs/ResumeRunEndpointTests.cs` — 10 integration tests (integration layer)
