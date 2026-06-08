# Interlude Transition Flow

**PR 0.2.0 — Backend Game Engine**

## Overview

The Interlude is a transitional state between two rooms. After defeating a room boss and selecting a reward, the player enters the Interlude hub before proceeding to the next room.

```
RoomBoss vaincu
    → Reward boss sélectionnée (SelectReward)
    → Run: RoomResolved
    → POST /api/v2/runs/{runId}/interlude/enter
    → Run: Interlude
    → GET  /api/v2/runs/{runId}/interlude   (hub nodes displayed)
    → POST /api/v2/runs/{runId}/rooms/next
    → Run: Active (new room generated, CurrentRoomIndex++)
```

---

## RunStatus transitions

| From | Trigger | To |
|---|---|---|
| `RoomResolved` | `Run.EnterInterlude()` | `Interlude` |
| `Interlude` | `Run.MoveToNextRoom(nextRoom)` | `Active` (or `BossReached` at depth 10) |

`Interlude` is value `7` in the `RunStatus` enum.

---

## Domain methods

### `Run.EnterInterlude()`

Guards (throws `DomainException` if violated):
- Run must not be Completed / Failed / Abandoned.
- `Status` must be `RoomResolved`.
- `HasActiveCombat` must be false.
- `HasPendingRewardOffer` must be false.

Effect: sets `Status = Interlude`. Does not increment `CurrentRoomIndex`.

### `Run.MoveToNextRoom(Room nextRoom)` — updated

Guard change: now requires `Status == Interlude` (previously accepted `RoomResolved`).

New effect: increments `CurrentRoomIndex` before setting status to `Active`.

---

## API endpoints

### `POST /api/v2/runs/{runId}/interlude/enter`

Triggers `EnterInterlude()` on the run. Returns `EnterInterludeResponse` containing an `InterludeDto` with the hub nodes and available actions.

### `GET /api/v2/runs/{runId}/interlude`

Returns the current interlude state. Requires `Status == Interlude`.

### `POST /api/v2/runs/{runId}/rooms/next`

Unchanged route, updated guard. Now requires `Status == Interlude`. Generates and enters the next room.

---

## InterludeNode model

`InterludeNode` is a distinct domain model — not a `MapNode`. It has no `riskLevel`, `rewardProfile`, `parentNodeIds`, `row`, or `lane`.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Unique identifier |
| `Type` | `InterludeNodeType` | Player / Elise / Inventory / Journal / Placeholder |
| `Label` | `string` | Display name |
| `Description` | `string` | Tooltip text |
| `PositionSlot` | `string` | Layout slot key |
| `IsEnabled` | `bool` | Whether the node is interactive |
| `ActionKey` | `InterludeActionKey` | Action to dispatch on interaction |

### Default hub layout (`DefaultInterludeNodeProvider`)

```
            [top: Journal]
[left: Elise]  [center: Player]  [right: Inventory]
    [bottom-left: Placeholder]  [bottom-right: Placeholder]
```

Both Placeholder nodes are `IsEnabled = false`. They are reserved for future mechanics.

---

## InterludeDto

Returned by both `EnterInterlude` and `GetInterlude`.

```json
{
  "runId": "...",
  "currentRoomIndex": 0,
  "displayRoomNumber": 1,
  "nodes": [ /* InterludeNodeDto[] */ ],
  "availableActions": [
    {
      "key": "enter-next-room",
      "label": "Reprendre la descente",
      "description": "Entrer dans la prochaine salle du Palais.",
      "requiresConfirmation": false,
      "isDangerous": false,
      "isEnabled": true
    }
  ],
  "runSummary": {
    "seed": "...",
    "currentRoomIndex": 0,
    "displayRoomNumber": 1,
    "currentRoomType": "Threshold",
    "activePalaceLawCount": 0
  }
}
```

---

## Guards added to existing handlers

During `Interlude`, the following commands are blocked with a `DomainException`:

| Handler | Guard message |
|---|---|
| `ProgressRunCommandHandler` | "Cannot progress: run is in Interlude. Navigate the interlude hub or enter the next room." |
| `ChooseNodeCommandHandler` | "Cannot choose a node: run is in Interlude. Navigate the interlude hub or enter the next room." |
| `ResolveCurrentEventCommandHandler` | "Cannot resolve an event: run is in Interlude. Navigate the interlude hub or enter the next room." |

---

## Out of scope (not in this PR)

- Database persistence of `RunStatus.Interlude`
- Save / exit from Interlude
- Inventory node interaction
- Elise dialogue
- Him'Lit / RunImprint mechanics
- Frontend implementation
- RoomMap refactor
