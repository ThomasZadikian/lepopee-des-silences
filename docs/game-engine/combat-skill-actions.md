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

## Non-objectives

This PR does not handle:

- Turn progression.
- Enemy AI.
- Initiative.
- Durable status effects.
- Automatic combat completion.
- Frontend behavior.
