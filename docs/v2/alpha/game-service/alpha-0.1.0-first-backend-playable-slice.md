# alpha-0.1.0 — First backend playable slice

## Goal
Validate the complete backend loop end-to-end through a single integration test: start a run → complete two full rooms (choose → resolve → combat → reward → progress → boss → room completed → next room).

## Changes

### Domain — `Run`
- **`CompleteRun(DateTimeOffset endedAt)`** — marks run as `Completed`.
- **`FailRun(DateTimeOffset endedAt)`** — marks run as `Failed`.
- **`EnsureActive()`** now explicitly rejects terminal states (`Completed`, `Failed`, `Abandoned`) with `"Run is closed."`.
- **`MoveToNextRoom(Room)`** now rejects closed runs before the room-completion check.

### Tests — `PlayableSliceTests`
- **`FullBackendLoop_ShouldCompleteMultipleRooms`** — new integration test:
  1. Start a run, validate depth 0, room Active, nodes available.
  2. Complete room 0 (loop: pick node → resolve event → combat → reward → progress → boss → completed).
  3. Assert room 0 is Completed, run status is RoomResolved, depth is 0.
  4. Move to next room (depth 1), assert Active status, correct depth, nodes available.
  5. Complete room 1 (same loop).
  6. Assert room 1 is Completed, run status is RoomResolved, depth 1.

### Tests — Unit (6 new)
- `CompleteRun_ShouldCloseRun_AsCompleted`
- `CompleteRun_ShouldThrow_WhenRunIsAlreadyClosed`
- `FailRun_ShouldCloseRun_AsFailed`
- `FailRun_ShouldThrow_WhenRunIsAlreadyClosed`
- `MoveToNextRoom_ShouldThrow_WhenRunIsCompleted`
- `MoveToNextRoom_ShouldThrow_WhenRunIsFailed`

## Test results
- 230 unit tests pass
- 29 integration tests pass (+1 playable slice)
- 3 consecutive full runs confirmed stable
