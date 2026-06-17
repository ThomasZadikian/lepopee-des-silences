# Follow-up: feat(game-client): display official combat metrics

**PR**: `feat(game-client): display official combat metrics`
**Target**: alpha-0.8.1
**Status**: Implemented

## What was done

Added a `CombatActionSummary` panel that displays the server-authoritative log entries from the last submitted action. No client-side metric calculation — all data comes directly from `CombatLogEntryDto.message`.

### Store changes (`useCombatStore.ts`)

- Added `lastActionEntries: ref<CombatLogEntryDto[]>([])` — holds entries from the most recent server response only (not cumulative).
- Reset at: action start (`submitAction`, `submitItemAction`), `initCombat`, `clearCombat`.
- Populated after `await playCombatLogs(response.logEntries)` in both action paths.

### New component (`CombatActionSummary.vue`)

Displays `lastActionEntries` with per-type color coding:

| Type | Color |
|---|---|
| SkillUsed / ItemUsed | `--gold` (action header) |
| DamageApplied | `--blood` |
| HealApplied | `--sap` |
| GuardGained | `--frost` |
| TargetDefeated | `--blood` + KO badge |
| TurnAdvanced / EnemyTurnResolved | `--ink-5` (dimmed) |
| CombatCompleted | `--gold` bold |
| CombatFailed | `--blood` bold |

Hidden when `entries` is empty (no v-if overhead between actions).

### Scene wiring (`CombatScene.vue`)

`CombatActionSummary` inserted between compose panel and `CombatLogPanel`. Grid updated from 4 to 5 explicit rows.

## Constraints respected

- Zero client-side damage/heal/guard calculation.
- No reconstruction of combat results from stats.
- All displayed values originate from `CombatLogEntryDto.message` (server string).
- `CombatMetersPanel` (client-calculated) untouched and still available via damage drawer.

## Tests

4 new Vitest cases in `useCombatStore.test.ts`:
- `lastActionEntries` starts empty
- cleared by `initCombat`
- cleared by `clearCombat`
- populated after `submitAction` completes
- replaced by subsequent `submitAction`
