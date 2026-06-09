# Combat skill actions

Combat actions are skill-first: an action is submitted with an actor, a skill key, and target ids. There is no separate basic attack action in the runtime contract.

Current endpoint:

`POST /api/v2/runs/{runId}/combats/{combatId}/skill-actions`

The endpoint validates the action, applies supported basic skill effects, and returns the updated combat runtime snapshot plus log entries.

## Targeting rules

`Self`

- Requires exactly one target.
- The target must be the actor.

`SingleEnemy`

- Requires exactly one target.
- The target must be on the opposite side from the actor.

`SingleAlly`

- Requires exactly one target.
- The target must be on the same side as the actor.

`AllEnemies`

- Requires explicit targets in this version.
- All targets must be on the opposite side from the actor.
- The submitted targets must cover all active enemies.

`AllAllies`

- Requires explicit targets in this version.
- All targets must be on the same side as the actor.
- The submitted targets must cover all active allies.

All targeting modes reject missing targets and defeated targets before applying the targeting type rule.

Unsupported targeting types return `Unsupported targeting type: {targetingType}`.

## Basic skill effect resolution

`Damage`

- Applies `BasePower` as raw damage.
- `Guard` absorbs damage before `CurrentVitality`.
- `CurrentVitality` never goes below zero.
- A target becomes `Defeated` when `CurrentVitality` reaches zero.

`Guard`

- Increases target `Guard` by `BasePower`.

`Weaken`

- Produces a log entry only in this version.
- Does not apply a durable status yet.

`Disrupt`

- Produces a log entry only in this version.
- Does not apply a durable status yet.

`ManaCost` and `ChargeCost` are defined on skill definitions, but they are not consumed in this PR. Runtime resource costs will be enforced later.

## Turn progression

After a valid action:

- The skill effect is applied.
- The combat checks victory or defeat.
- If the combat continues, the turn advances to the next active combatant.
- Defeated combatants are skipped.
- `TurnNumber` increases when the cycle returns to the first active combatant.

Only the current `ActiveCombatantId` can submit an action. Actions from another combatant are rejected with `It is not this combatant's turn.`

The turn order is deterministic: allies first, then enemies, in creation order.

## Enemy turns

When a player action is resolved, the Game Engine advances the turn.

If the next active combatant is an enemy:

- The backend resolves the enemy action automatically.
- The enemy chooses a skill deterministically.
- The enemy targets the first active ally.
- The backend continues resolving consecutive enemy turns until the next active ally or combat end.
- A safety limit stops automatic resolution after `living combatants + 1` enemy resolutions.

Enemy logs are returned in the same action response as the player logs.

## Current enemy AI

The current enemy AI is intentionally minimal:

- Deterministic.
- No ATB.
- No threat calculation.
- No advanced strategy.
- No adaptive behavior.

Skill selection is stable: offensive skills first, then `Damage`, then the first available skill ordered by key.

## ATB out of scope

ATB is intentionally out of scope before `0.4.0`. It will be specified separately later.

## Non-objectives

This PR does not handle:

- Advanced enemy AI.
- Initiative.
- Advanced initiative.
- Individual speed.
- Durable status effects.
- Full run progression after combat victory.
- Frontend behavior.
