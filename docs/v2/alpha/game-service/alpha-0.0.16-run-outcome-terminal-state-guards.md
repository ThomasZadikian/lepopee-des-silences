# alpha-0.0.16 — Run outcome (CompleteRun, FailRun, terminal-state guards)

## Goal
Add domain methods to mark a run as completed or failed, and guard all mutating operations against terminal states (Completed, Failed, Abandoned).

## Changes

### Domain — `Run`
- **`CompleteRun(DateTimeOffset endedAt)`** — marks the run as `RunStatus.Completed`; throws if already closed.
- **`FailRun(DateTimeOffset endedAt)`** — marks the run as `RunStatus.Failed`; throws if already closed.
- **`EnsureActive()`** — now explicitly checks for terminal states (`Completed`, `Failed`, `Abandoned`) with a "Run is closed." message before the generic "Run must be active." check.
- **`MoveToNextRoom(Room)`** — added early check: if the run is `Completed`, `Failed`, or `Abandoned`, throws `"Run is closed."` before the existing room-completion check.

### Tests — Unit (6 new)
| Test | What it verifies |
|------|-----------------|
| `CompleteRun_ShouldCloseRun_AsCompleted` | Status = Completed, EndedAt set |
| `CompleteRun_ShouldThrow_WhenRunIsAlreadyClosed` | Second call throws |
| `FailRun_ShouldCloseRun_AsFailed` | Status = Failed, EndedAt set |
| `FailRun_ShouldThrow_WhenRunIsAlreadyClosed` | Second call throws |
| `MoveToNextRoom_ShouldThrow_WhenRunIsCompleted` | Terminal-state guard fires |
| `MoveToNextRoom_ShouldThrow_WhenRunIsFailed` | Terminal-state guard fires |

## Test results
- 230 unit tests pass (+6 new)
- 28 integration tests pass (unchanged)
- 3 consecutive full runs confirmed stable
