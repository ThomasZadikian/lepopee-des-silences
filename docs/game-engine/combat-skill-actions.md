# Combat skill actions

Combat actions are skill-first: an action is submitted with an actor, a skill key, and target ids. There is no separate basic attack action in the runtime contract.

Current endpoint:

`POST /api/v2/runs/{runId}/combats/{combatId}/skill-actions`

The endpoint validates the action and returns the unchanged combat runtime snapshot plus an action-accepted log entry. It does not resolve skill effects yet.

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

This PR validates targets but does not resolve skill effects, damage, guard, weaken, disrupt, turns, combat completion, or enemy AI.
