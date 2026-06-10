# Combat API Contracts

## Resolve Current Event

```
POST /api/v2/runs/{runId}/current-event/resolve
```

Resolves the currently selected node event. When the event is a combat encounter, the response contains both the encounter draft and the runtime combat.

### Response (combat encounter)

```json
{
  "run": { /* RunDto */ },
  "outcome": { /* ResolvedNodeEventOutcomeDto */ },
  "encounterDraft": { /* CombatEncounterDraftDto or null */ },
  "combat": { /* CombatRuntimeDto or null */ }
}
```

### HTTP Status Codes

| Code | Condition |
|------|-----------|
| 200 | Event resolved successfully |
| 400 | Domain rule violated (e.g., no node selected, run in Interlude, combat already active) |
| 404 | Run not found |

### Notes

- `encounterDraft` may be null if draft generation fails silently
- `combat` may be null if draft generation fails; use `GET /current-combat` as fallback
- After a combat victory, resolving the same event again returns 400 (combat already active)

---

## Get Current Combat

```
GET /api/v2/runs/{runId}/current-combat
```

Returns the active runtime combat for the run.

### Response

```json
{
  "id": "guid",
  "status": "Active | Completed | Failed",
  "turnNumber": 1,
  "activeCombatantId": "guid or null",
  "allies": [ /* CombatantRuntimeDto[] */ ],
  "enemies": [ /* CombatantRuntimeDto[] */ ]
}
```

### CombatantRuntimeDto

```json
{
  "id": "guid",
  "sourceKey": "string",
  "displayName": "string",
  "side": "Player | Enemy",
  "archetype": "string",
  "maxVitality": 100,
  "currentVitality": 80,
  "guard": 0,
  "mana": 0,
  "charge": 0,
  "status": "Active | Defeated",
  "skills": [ /* CombatantSkillRuntimeDto[] */ ]
}
```

### CombatantSkillRuntimeDto

```json
{
  "key": "skill.basic.strike",
  "displayName": "Frappe",
  "skillType": "Damage",
  "targetingType": "SingleEnemy",
  "effectType": "Damage",
  "manaCost": 0,
  "chargeCost": 0,
  "basePower": 10,
  "tags": []
}
```

### HTTP Status Codes

| Code | Condition |
|------|-----------|
| 200 | Active combat found |
| 404 | Run not found or no active combat |

### Notes

- After combat completion, `GET /current-combat` returns 404 (active combat is cleared)
- Allies always appear before enemies in the combatant lists
- Turn order is deterministic: all allies (in order), then all enemies (in order)

---

## Use Combat Skill

```
POST /api/v2/runs/{runId}/combats/{combatId}/skill-actions
```

Submit a skill-based action for the current combat.

### Request

```json
{
  "actorId": "guid",
  "skillKey": "skill.basic.strike",
  "targetIds": ["guid"]
}
```

### Response

```json
{
  "combatId": "guid",
  "actorId": "guid",
  "skillKey": "string",
  "targetIds": ["guid"],
  "accepted": true,
  "message": null,
  "combat": { /* CombatRuntimeDto */ },
  "logEntries": [ /* CombatLogEntryDto[] */ ],
  "combatCompleted": false,
  "combatFailed": false,
  "canProgressRun": false,
  "runStatus": "Active | Failed"
}
```

### CombatLogEntryDto

```json
{
  "occurredAtUtc": "2026-01-01T00:00:00Z",
  "type": "SkillUsed | DamageApplied | GuardGained | TargetDefeated | TurnAdvanced | EnemyTurnResolved | CombatCompleted | CombatFailed",
  "message": "string",
  "actorId": "guid or null",
  "skillKey": "string or null",
  "targetIds": ["guid"]
}
```

### HTTP Status Codes

| Code | Condition |
|------|-----------|
| 200 | Action accepted and processed |
| 400 | Payload invalid (FluentValidation), domain invariant violated (e.g., wrong turn via domain validation) |
| 404 | Run not found |
| 409 | Business rule conflict (e.g., wrong combatId, combat already completed, run not active, no active combat, invalid target, skill not owned, actor not active combatant) |

### Targeting Rules

| Targeting Type | Valid Targets |
|----------------|---------------|
| `Self` | Only the actor itself |
| `SingleEnemy` | One enemy from the opposite side |
| `SingleAlly` | One ally from the same side |
| `AllEnemies` | All active enemies on the opposite side |
| `AllAllies` | All active allies on the same side |

### Notes

- After a player action, all consecutive enemy turns are auto-resolved before the API responds
- The response always returns a stable state where the active combatant is an ally (or combat is completed/failed)
- `combatCompleted` and `combatFailed` indicate combat terminal states
- `canProgressRun` is true only when combat completed successfully
- `runStatus` reflects the current run status after the action

---

## Multi-Enemy Support

- Combat supports any number of enemies and allies
- Turn order: all allies (in order added), then all enemies (in order added)
- Defeated combatants are skipped during turn progression
- Combat completes only when ALL enemies are defeated
- Combat fails when ALL allies are defeated

---

## Idempotence

- Resolving the same event while combat is active returns 400
- Sending a skill action after combat completed returns 409
- Sending a skill action with a wrong combatId returns 409
- GET current-combat returns 404 after completion (active combat cleared)

---

## Error Code Convention

| HTTP Code | Category | Source |
|-----------|----------|--------|
| 400 | Technically invalid request | FluentValidation errors, DomainException from domain invariants |
| 404 | Resource not found | NotFoundException (run, combat, etc.) |
| 409 | Business rule conflict | ConflictException (state mismatch, wrong turn, combat already done, etc.) |
| 500 | Unexpected error | Unhandled exceptions (logged) |

### Rationale

- **400 BadRequest**: The request itself is malformed or violates domain invariants (e.g., empty actorId, skill key, trying to act on a defeated combatant from domain model)
- **409 Conflict**: The request is technically valid but conflicts with the current state of the resource (e.g., combat already completed, wrong combatId, run not active)

---

## Frontend Expectations

The frontend should be able to:

1. **Display active combatant**: use `activeCombatantId` to highlight the current actor
2. **Display HP/Guard**: use `currentVitality`, `maxVitality`, `guard` on each combatant
3. **Display available skills**: use `skills` array on each combatant
4. **Select a skill**: pick a skill by `key`
5. **Select valid targets**: use `targetingType` to determine valid targets
6. **Send an action**: POST to `/skill-actions` with `actorId`, `skillKey`, `targetIds`
7. **Read logs**: parse `logEntries` to display action results
8. **Detect victory**: check `combatCompleted === true`
9. **Detect defeat**: check `combatFailed === true`
10. **Resume progression**: after victory, resolve current event again to continue
