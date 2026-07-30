import { describe, expect, it } from 'vitest';

import {
  BASE_STEP_MS,
  dynamicStepDurationMs,
  ENEMY_STEP_MULTIPLIER,
  useCombatPlayback,
} from './useCombatPlayback';

describe('tactical combat playback', () => {
  it('keeps the authored timing curve for short, medium and long paths', () => {
    expect(dynamicStepDurationMs(2)).toBe(Math.floor(BASE_STEP_MS * 0.7));
    expect(dynamicStepDurationMs(3)).toBe(BASE_STEP_MS);
    expect(dynamicStepDurationMs(5)).toBe(Math.floor(BASE_STEP_MS * 1.3));
  });

  it('slows enemies without guessing their side from a UUID', () => {
    const enemyBase = Math.floor(BASE_STEP_MS * ENEMY_STEP_MULTIPLIER);
    expect(dynamicStepDurationMs(3, true)).toBe(enemyBase);
    expect(dynamicStepDurationMs(3, false)).toBe(BASE_STEP_MS);
  });

  it('interpolates an ally with the side carried by the playback event', () => {
    const playback = useCombatPlayback();
    playback.walk.value = {
      combatantId: '8e9d05b4-fba0-49fb-81bf-ef88ab0db35a',
      path: [{ x: 0, y: 0 }, { x: 1, y: 0 }],
      startedAt: 100,
      isEnemy: false,
    };

    expect(playback.positionOf(
      '8e9d05b4-fba0-49fb-81bf-ef88ab0db35a',
      { x: 1, y: 0 },
      100 + (dynamicStepDurationMs(1, false) / 2),
    ).x).toBeCloseTo(0.5);
  });
});
