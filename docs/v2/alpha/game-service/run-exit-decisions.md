# Run Exit Decisions — Save / Abandon / Return-to-menu

**PR:** 0.2.1  
**Status:** Implemented

---

## Overview

When a player reaches a safe point in a run, three exit decisions become available:

| Action | Key | Result | Reversible |
|---|---|---|---|
| Continue | `enter-next-room` | Enters next room | — |
| Save & Exit | `save-and-exit` | Suspends run, returns to menu | Yes |
| Abandon | `abandon-run` | Permanently ends run | No |

Safe points are the only states from which Save & Exit and Abandon are permitted.

---

## Safe Points

A **safe point** is a run state in which no game action is in progress and the player can safely leave:

- `RunStatus.RoomResolved` — boss defeated, reward collected, room complete
- `RunStatus.Interlude` — in the interlude hub between rooms

An `Active` run (mid-room, mid-combat, pending reward) is **not** a safe point.

---

## RunStatus Extended

```
Created=0, Active=1, RoomResolved=2, BossReached=3,
Completed=4, Failed=5, Abandoned=6, Interlude=7, Suspended=8
```

`Suspended` is new in 0.2.1. A suspended run:
- was paused at a safe point by the player
- can be resumed (client reads `CanResume = true` on `RunDto`)
- accepts no game actions until resumed

---

## SaveAndExit

### Domain: `Run.SaveAndExit(DateTimeOffset savedAt)`

Guards (in order):
1. Run is not closed or already suspended → `"Run is closed or already suspended."`
2. Status is `RoomResolved` or `Interlude` → `"Cannot save and exit: run must be at a safe point (RoomResolved or Interlude)."`
3. No active combat → `"Cannot save and exit: run has an active combat."`
4. No pending reward → `"Cannot save and exit: run has a pending reward offer that must be selected first."`

Effect:
- `Status = Suspended`
- `SavedAt = savedAt`

### Application: `SaveAndExitRunCommandHandler`

Route: `POST /api/v2/runs/{runId}/save-and-exit`

Delegates entirely to the domain method. No handler-level guard needed — all logic lives in the domain.

### RunDto fields added

| Field | Type | Description |
|---|---|---|
| `CanResume` | `bool` | `true` when `Status == Suspended` |
| `SavedAt` | `DateTimeOffset?` | Set by `SaveAndExit`; `null` if never suspended |
| `AbandonedAt` | `DateTimeOffset?` | Equals `EndedAt` when `Status == Abandoned`; otherwise `null` |

---

## AbandonRun

### Handler-level safe-point guard

`AbandonRunCommandHandler` now enforces that the run must be at a safe point before calling `run.Abandon()`:

```csharp
if (run.Status is not (RunStatus.RoomResolved or RunStatus.Interlude))
{
    throw new DomainException(
        "AbandonRun is only allowed from a safe point (RoomResolved or Interlude).");
}
```

The domain's `Abandon()` method is unchanged — it still throws `"Run is already closed."` if called on a Completed/Failed/Abandoned run.

Route: `POST /api/v2/runs/{runId}/abandon`

---

## Interlude Actions

`InterludeDto.BuildDefaultActions()` now returns all three actions:

```
enter-next-room   RequiresConfirmation=false  IsDangerous=false
save-and-exit     RequiresConfirmation=false  IsDangerous=false
abandon-run       RequiresConfirmation=true   IsDangerous=true
```

`abandon-run` is marked `IsDangerous=true` and `RequiresConfirmation=true` because it is irreversible.

---

## Guards Added

All commands that call `EnsureActive()` now get a specific message for Suspended runs:

> `"Run is suspended and cannot accept game actions until resumed."`

Previously, Suspended would have silently fallen through to the generic `"Run must be active."` — this change makes the client reason explicit.

`EnterInterlude()` and `MoveToNextRoom()` now include `Suspended` in their "closed" check, so a suspended run cannot re-enter the interlude or move to the next room without resuming first.

---

## State Transitions

```
Active → (boss defeated + reward selected) → RoomResolved
RoomResolved → (enter interlude) → Interlude

RoomResolved │
Interlude    ├──→ SaveAndExit  → Suspended
             ├──→ AbandonRun   → Abandoned (terminal)
             └──→ EnterNextRoom → Active (next room)

Suspended → (resume, future PR) → RoomResolved or Interlude
```

---

## Files Changed

**Domain**
- `RunStatus.cs` — added `Suspended = 8`
- `Run.cs.cs` — added `SavedAt`, `SaveAndExit()`, updated guards in `EnterInterlude()`, `MoveToNextRoom()`, `EnsureActive()`

**Application**
- `Runs/SaveAndExitRun/` — new command / handler / validator / response
- `Runs/AbandonRun/AbandonRunCommandHandler.cs` — added safe-point guard
- `Runs/Dtos/RunDto.cs` — added `CanResume`, `SavedAt`, `AbandonedAt`
- `Interlude/Dtos/InterludeDto.cs` — `BuildDefaultActions()` now returns all 3 actions

**API**
- `RunsController.cs` — added `POST .../save-and-exit` endpoint

**Tests**
- `Runs/RunExitDecisionsTests.cs` — 31 tests (Groups A–D)
- `Runs/AbandonRun/AbandonRunCommandHandlerTests.cs` — updated to reflect safe-point guard
