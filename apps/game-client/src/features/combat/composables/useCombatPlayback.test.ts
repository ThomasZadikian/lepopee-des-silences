import { describe, expect, it } from 'vitest';

import {
  BASE_STEP_MS,
  dynamicStepDurationMs,
  ENEMY_STEP_MULTIPLIER,
  TURN_TRANSITION_MS,
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

describe('annonce du geste adverse', () => {
  const enemyId = '11111111-1111-1111-1111-111111111111';
  const allyId = '22222222-2222-2222-2222-222222222222';

  const stateWith = (allies: string[]) => ({
    allies: allies.map((id) => ({ combatant: { id } })),
    enemies: [],
  } as never);

  const skillEvent = (actorId: string, overrides = {}) => ({
    kind: 'Skill' as const,
    actorId,
    actorName: 'Sentinelle',
    path: [],
    skillKey: 'skill.strike',
    skillName: 'Frappe',
    targetX: 2,
    targetY: 3,
    impacts: [],
    telegraphCells: [{ x: 2, y: 3 }, { x: 3, y: 3 }],
    ...overrides,
  });

  it('allume la zone adverse avant le geste, puis l’éteint', async () => {
    const playback = useCombatPlayback();
    const seen: Array<number> = [];

    const running = playback.play([skillEvent(enemyId)], stateWith([allyId]), () => 0);

    // Relevé pendant l'annonce : la zone doit être lisible AVANT la résolution. On laisse
    // passer la transition de tour (TURN_TRANSITION_MS), qui précède l'annonce.
    await new Promise((resolve) => setTimeout(resolve, TURN_TRANSITION_MS + 100));
    seen.push(playback.telegraph.value?.cells.length ?? 0);
    expect(playback.telegraph.value?.label).toContain('prépare');

    await running;
    seen.push(playback.telegraph.value?.cells.length ?? 0);

    expect(seen).toEqual([2, 0]);
  });

  it('n’annonce jamais le geste d’un allié : le joueur vient de le commander', async () => {
    const playback = useCombatPlayback();

    const running = playback.play([skillEvent(allyId)], stateWith([allyId]), () => 0);
    await new Promise((resolve) => setTimeout(resolve, 50));


    expect(playback.telegraph.value).toBeNull();
    await running;
  });
});

describe('vitalité affichée pendant la lecture', () => {
  const enemyAId = '55555555-5555-5555-5555-555555555555';
  const enemyBId = '66666666-6666-6666-6666-666666666666';
  const allyId = '77777777-7777-7777-7777-777777777777';

  const hitEvent = (actorId: string, delta: number) => ({
    kind: 'Skill' as const,
    actorId,
    actorName: 'Ennemi',
    path: [],
    skillKey: 'skill.strike',
    skillName: 'Frappe',
    targetX: 1,
    targetY: 1,
    telegraphCells: null,
    impacts: [
      { combatantId: allyId, x: 1, y: 1, vitalityDelta: delta, defeated: false, missed: false },
    ],
  });

  it('ne montre pas le total final avant que le premier coup n’ait atterri', async () => {
    const playback = useCombatPlayback();
    // Deux ennemis, 18 PV chacun : `combat.value` (donc `finalState` ici) arrive déjà
    // décrémenté des deux coups — c'est le bug remonté : la barre ne doit perdre les 36 PV
    // que progressivement, jamais les deux d'un coup au tout début du tour.
    const finalState = {
      allies: [{ combatant: { id: allyId, currentVitality: 64 } }],
      enemies: [],
    } as never;

    const running = playback.play(
      [hitEvent(enemyAId, 18), hitEvent(enemyBId, 18)],
      finalState,
      () => 0,
    );

    // La chronologie n'a encore rien joué (le premier `await wait(...)` n'a pas résolu), mais
    // le relevé de départ, lui, est déjà posé de façon synchrone.
    expect(playback.vitalsOf(allyId, 64)).toBe(100);

    await running;

    expect(playback.vitalsOf(allyId, 64)).toBe(64);
  });
});

describe('coup manqué', () => {
  it('écrit « Manqué » plutôt qu’un zéro, et sans onde d’impact', async () => {
    const playback = useCombatPlayback();
    const enemyId = '33333333-3333-3333-3333-333333333333';
    const allyId = '44444444-4444-4444-4444-444444444444';

    await playback.play(
      [{
        kind: 'Skill' as const,
        actorId: enemyId,
        actorName: 'Sentinelle',
        path: [],
        skillKey: 'skill.strike',
        skillName: 'Frappe',
        targetX: 1,
        targetY: 1,
        telegraphCells: null,
        impacts: [
          { combatantId: allyId, x: 1, y: 1, vitalityDelta: 0, defeated: false, missed: true },
        ],
      }] as never,
      { allies: [{ combatant: { id: allyId } }], enemies: [] } as never,
      () => 0,
    );

    expect(playback.floats.value).toHaveLength(1);
    expect(playback.floats.value[0].text).toBe('Manqué');
    // Rien n'a été percuté : aucune onde ne doit partir de la case.
    expect(playback.impacts.value).toHaveLength(0);
  });
});
