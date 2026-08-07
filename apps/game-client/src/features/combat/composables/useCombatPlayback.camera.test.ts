import { afterEach, describe, expect, it, vi } from 'vitest';

import { useCombatPlayback } from './useCombatPlayback';

afterEach(() => {
  vi.useRealTimers();
});

describe('combat camera sequencing', () => {
  it('finishes camera framing and the spell before spawning impacts', async () => {
    vi.useFakeTimers();

    const playback = useCombatPlayback();
    const actorId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
    const targetId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
    const order: string[] = [];
    let finishSort: (() => void) | null = null;

    playback.setSceneTransitionWaiter(async () => {
      order.push('scene');
    });
    playback.setCameraAnimator(async (cue) => {
      order.push(`camera:${cue.kind}`);
    });
    playback.setSortAnimator(async () => {
      order.push('sort');
      await new Promise<void>((resolve) => { finishSort = resolve; });
      order.push('sort-finished');
    });

    const running = playback.play(
      [{
        kind: 'Skill',
        actorId,
        actorName: 'Allié',
        path: [],
        skillKey: 'canon.skill.fondations-de-thomas',
        skillName: 'Fondations',
        targetX: 5,
        targetY: 4,
        telegraphCells: null,
        impacts: [{
          combatantId: targetId,
          x: 5,
          y: 4,
          vitalityDelta: 12,
          defeated: false,
          missed: false,
        }],
      }],
      {
        allies: [{
          x: 2,
          y: 2,
          combatant: { id: actorId, currentVitality: 100 },
        }],
        enemies: [{
          x: 5,
          y: 4,
          combatant: { id: targetId, currentVitality: 88 },
        }],
      } as never,
      () => 0,
    );

    // Laisse passer les awaits synchrones de transition/caméra jusqu'au sort bloquant.
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();

    expect(order).toEqual(['scene', 'camera:actor', 'camera:action', 'sort']);
    expect(playback.impacts.value).toHaveLength(0);

    finishSort!();
    await Promise.resolve();

    expect(order).toEqual([
      'scene',
      'camera:actor',
      'camera:action',
      'sort',
      'sort-finished',
    ]);
    expect(playback.impacts.value).toHaveLength(1);

    await vi.runAllTimersAsync();
    await running;
  });
});

describe('scene transition barrier', () => {
  it('starts no camera or combat animation before the bottom bar transition has finished', async () => {
    const playback = useCombatPlayback();
    const order: string[] = [];
    let releaseScene: (() => void) | null = null;

    playback.setSceneTransitionWaiter(async () => {
      order.push('scene-start');
      await new Promise<void>((resolve) => { releaseScene = resolve; });
      order.push('scene-finished');
    });
    playback.setCameraAnimator(async (cue) => {
      order.push(`camera:${cue.kind}`);
    });
    playback.setSortAnimator(async () => {
      order.push('sort');
    });

    const actorId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
    const running = playback.play(
      [{
        kind: 'Skill',
        actorId,
        actorName: 'Allié',
        path: [],
        skillKey: 'canon.skill.fondations-de-thomas',
        skillName: 'Fondations',
        targetX: 4,
        targetY: 2,
        telegraphCells: null,
        impacts: [],
      }],
      {
        allies: [{ x: 1, y: 1, combatant: { id: actorId, currentVitality: 100 } }],
        enemies: [],
      } as never,
      () => 0,
    );

    await Promise.resolve();
    expect(order).toEqual(['scene-start']);

    releaseScene!();
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();

    expect(order.slice(0, 3)).toEqual(['scene-start', 'scene-finished', 'camera:actor']);

    await running;
  });
});

describe('camera continuity across one actor sequence', () => {
  it('does not recenter the same actor between Move and Skill', async () => {
    vi.useFakeTimers();

    const playback = useCombatPlayback();
    const actorId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
    const cues: string[] = [];

    playback.setCameraAnimator(async (cue) => {
      cues.push(cue.kind);
    });
    playback.setSortAnimator(async () => {});

    const running = playback.play(
      [
        {
          kind: 'Move',
          actorId,
          actorName: 'Allié',
          path: [{ x: 3, y: 2 }],
          skillKey: null,
          skillName: null,
          targetX: null,
          targetY: null,
          telegraphCells: null,
          impacts: [],
        },
        {
          kind: 'Skill',
          actorId,
          actorName: 'Allié',
          path: [],
          skillKey: 'canon.skill.fondations-de-thomas',
          skillName: 'Fondations',
          targetX: 4,
          targetY: 2,
          telegraphCells: null,
          impacts: [],
        },
      ],
      {
        allies: [{ x: 3, y: 2, combatant: { id: actorId, currentVitality: 100 } }],
        enemies: [],
      } as never,
      () => 0,
    );

    await vi.runAllTimersAsync();
    await running;

    expect(cues).toEqual(['actor', 'action']);
  });
});
