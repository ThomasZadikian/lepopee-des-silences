import { describe, expect, it } from 'vitest';

import {
  BASE_STEP_MS,
  dynamicStepDurationMs,
  ENEMY_STEP_MULTIPLIER,
  TICK_IMPACT_STAGGER_MS,
  TICK_SETTLE_MS,
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

  it('holds an ally on its cell until the full step duration elapses, then jumps — no continuous glide', () => {
    const playback = useCombatPlayback();
    playback.walk.value = {
      combatantId: '8e9d05b4-fba0-49fb-81bf-ef88ab0db35a',
      path: [{ x: 0, y: 0 }, { x: 1, y: 0 }],
      startedAt: 100,
      isEnemy: false,
    };

    const stepMs = dynamicStepDurationMs(1, false);

    // Aligned with usePartyTokenPath (exploration): case-by-case stepping, never a fractional
    // in-between position — this is the mid-step read that used to report x≈0.5.
    expect(playback.positionOf(
      '8e9d05b4-fba0-49fb-81bf-ef88ab0db35a',
      { x: 1, y: 0 },
      100 + (stepMs / 2),
    ).x).toBe(0);

    expect(playback.positionOf(
      '8e9d05b4-fba0-49fb-81bf-ef88ab0db35a',
      { x: 1, y: 0 },
      100 + stepMs,
    ).x).toBe(1);
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

describe('états actifs affichés pendant la lecture', () => {
  const enemyId = '88888888-8888-8888-8888-888888888888';
  const allyId = '99999999-9999-9999-9999-999999999999';

  const hitEvent = (overrides = {}) => ({
    kind: 'Skill' as const,
    actorId: enemyId,
    actorName: 'Ennemi',
    path: [],
    skillKey: 'skill.poison',
    skillName: 'Poison',
    targetX: 1,
    targetY: 1,
    telegraphCells: null,
    impacts: [],
    ...overrides,
  });

  const dotStatus = (stacks: number) => ([{
    key: 'status.poison',
    displayName: 'Poison',
    kind: 'DamageOverTime' as const,
    stat: 'None',
    magnitude: 0,
    stacks,
    isMagnitudePercentOfBaseStat: false,
    perTickAmount: 4,
    ticksRemaining: 3000,
    isPermanent: false,
  }]);

  it('ne montre pas un nouveau stack avant que le geste qui l’applique n’ait fini de jouer', async () => {
    const playback = useCombatPlayback();

    // État précédent : un seul stack de Poison sur l'allié.
    playback.pinBefore({
      allies: [{ x: 1, y: 1, combatant: { id: allyId, statusEffects: dotStatus(1) } }],
      enemies: [],
    } as never);

    // Le nouvel état (déjà reçu du serveur) porte le deuxième stack — c'est le bug remonté :
    // il ne doit apparaître qu'une fois le geste de l'ennemi effectivement joué à l'écran.
    const finalState = {
      allies: [{ combatant: { id: allyId, statusEffects: dotStatus(2) } }],
      enemies: [],
    } as never;

    const running = playback.play([hitEvent()], finalState, () => 0);

    expect(playback.statusEffectsOf(allyId, dotStatus(2))[0].stacks).toBe(1);

    await running;

    expect(playback.statusEffectsOf(allyId, dotStatus(2))[0].stacks).toBe(2);
  });

  it('relâche immédiatement le gel quand il n’y a aucun événement à rejouer', async () => {
    const playback = useCombatPlayback();
    playback.pinBefore({
      allies: [{ x: 1, y: 1, combatant: { id: allyId, statusEffects: dotStatus(1) } }],
      enemies: [],
    } as never);

    await playback.play([], { allies: [], enemies: [] } as never, () => 0);

    expect(playback.statusEffectsOf(allyId, dotStatus(2))[0].stacks).toBe(2);
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

describe('garde absorbée — un coup entièrement encaissé n’est plus invisible', () => {
  const enemyId = '55554444-5555-4444-5555-444455554444';
  const allyId = '66665555-6666-5555-6666-555566665555';

  const guardEvent = (impacts: unknown[]) => ({
    kind: 'Skill' as const,
    actorId: enemyId,
    actorName: 'Sentinelle',
    path: [],
    skillKey: 'skill.strike',
    skillName: 'Frappe',
    targetX: 1,
    targetY: 1,
    telegraphCells: null,
    impacts,
  });

  it('fait s’envoler un chiffre de garde, jamais un « −0 », quand un coup est entièrement absorbé', async () => {
    const playback = useCombatPlayback();

    await playback.play(
      [guardEvent([
        { combatantId: allyId, x: 1, y: 1, vitalityDelta: 0, defeated: false, missed: false, guardAbsorbed: 8 },
      ])] as never,
      { allies: [{ combatant: { id: allyId } }], enemies: [] } as never,
      () => 0,
    );

    expect(playback.floats.value).toHaveLength(1);
    expect(playback.floats.value[0].text).toBe('−8');
    expect(playback.floats.value[0].kind).toBe('guard');
  });

  it('montre le chiffre de garde ET le chiffre de dégâts quand un coup déborde sur la vitalité', async () => {
    const playback = useCombatPlayback();

    await playback.play(
      [guardEvent([
        { combatantId: allyId, x: 1, y: 1, vitalityDelta: 7, defeated: false, missed: false, guardAbsorbed: 5 },
      ])] as never,
      { allies: [{ combatant: { id: allyId } }], enemies: [] } as never,
      () => 0,
    );

    const texts = playback.floats.value.map((f) => `${f.kind ?? 'damage'}:${f.text}`);
    expect(texts).toEqual(expect.arrayContaining(['guard:−5', 'damage:−7']));
  });

  it('ne montre aucun chiffre de garde quand rien n’a été absorbé', async () => {
    const playback = useCombatPlayback();

    await playback.play(
      [guardEvent([
        { combatantId: allyId, x: 1, y: 1, vitalityDelta: 6, defeated: false, missed: false, guardAbsorbed: 0 },
      ])] as never,
      { allies: [{ combatant: { id: allyId } }], enemies: [] } as never,
      () => 0,
    );

    expect(playback.floats.value).toHaveLength(1);
    expect(playback.floats.value[0].kind ?? 'damage').toBe('damage');
  });

  it('reste rétro-compatible avec une réponse serveur qui ne porte pas encore guardAbsorbed', async () => {
    const playback = useCombatPlayback();

    await playback.play(
      [guardEvent([
        { combatantId: allyId, x: 1, y: 1, vitalityDelta: 4, defeated: false, missed: false },
      ])] as never,
      { allies: [{ combatant: { id: allyId } }], enemies: [] } as never,
      () => 0,
    );

    expect(playback.floats.value).toHaveLength(1);
    expect(playback.floats.value[0].kind ?? 'damage').toBe('damage');
  });
});

describe('tick de DoT/HoT — pas de décision d’acteur, mais un chiffre s’envole quand même', () => {
  const enemyId = '10101010-1010-1010-1010-101010101010';
  const allyId = '20202020-2020-2020-2020-202020202020';

  const tickEvent = (overrides = {}) => ({
    kind: 'Tick' as const,
    actorId: enemyId,
    actorName: 'Empoisonné',
    path: [],
    skillKey: null,
    skillName: null,
    targetX: null,
    targetY: null,
    telegraphCells: null,
    impacts: [
      // Same sign convention as any other impact: positive delta = damage, negative = healed.
      { combatantId: enemyId, x: 2, y: 2, vitalityDelta: 5, defeated: false, missed: false },
    ],
    ...overrides,
  });

  it('fait s’envoler un chiffre de dégâts pour le tick, sans annonce ni bannière d’action', async () => {
    const playback = useCombatPlayback();

    const running = playback.play(
      [tickEvent()],
      { allies: [{ combatant: { id: allyId } }], enemies: [{ combatant: { id: enemyId } }] } as never,
      () => 0,
    );

    // Le tick n'a pas de zone à annoncer : aucun temps de réflexion, aucune bannière — il vient
    // du même souffle que la transition de tour, pas d'une décision qu'on lirait avant coup.
    expect(playback.telegraph.value).toBeNull();
    expect(playback.actionBanner.value).toBeNull();

    await running;

    expect(playback.floats.value).toHaveLength(1);
    expect(playback.floats.value[0].text).toBe('−5');
    expect(playback.impacts.value).toHaveLength(1);
  });

  it('inflige bien le delta à la vitalité affichée, comme un impact normal', async () => {
    const playback = useCombatPlayback();

    await playback.play(
      [tickEvent()],
      { allies: [], enemies: [{ combatant: { id: enemyId, currentVitality: 25 } }] } as never,
      () => 0,
    );

    expect(playback.vitalsOf(enemyId, 25)).toBe(25);
  });

  it('marque un soin de tick en vert avec un signe explicite', async () => {
    const playback = useCombatPlayback();

    await playback.play(
      [tickEvent({
        impacts: [
          { combatantId: enemyId, x: 2, y: 2, vitalityDelta: -6, defeated: false, missed: false },
        ],
      })],
      { allies: [], enemies: [{ combatant: { id: enemyId } }] } as never,
      () => 0,
    );

    expect(playback.floats.value[0].text).toBe('+6');
  });

  it('marque une pause de lecture après le tick avant d’enchaîner', async () => {
    const playback = useCombatPlayback();
    const start = performance.now();

    await playback.play(
      [tickEvent()],
      { allies: [], enemies: [{ combatant: { id: enemyId } }] } as never,
      () => 0,
    );

    expect(performance.now() - start).toBeGreaterThanOrEqual(TICK_SETTLE_MS - 20);
  });

  it('fait s’envoler un chiffre distinct pour chaque impact d’un même tick, plutôt que de les fusionner', async () => {
    const playback = useCombatPlayback();

    await playback.play(
      [tickEvent({
        impacts: [
          { combatantId: enemyId, x: 2, y: 2, vitalityDelta: 5, defeated: false, missed: false },
          { combatantId: enemyId, x: 2, y: 2, vitalityDelta: 3, defeated: false, missed: false },
        ],
      })],
      { allies: [], enemies: [{ combatant: { id: enemyId } }] } as never,
      () => 0,
    );

    expect(playback.floats.value).toHaveLength(2);
    expect(playback.floats.value.map((f) => f.text)).toEqual(['−5', '−3']);
  });

  it('attend TICK_IMPACT_STAGGER_MS entre deux impacts du même tick avant d’enchaîner', async () => {
    const playback = useCombatPlayback();
    const start = performance.now();

    await playback.play(
      [tickEvent({
        impacts: [
          { combatantId: enemyId, x: 2, y: 2, vitalityDelta: 5, defeated: false, missed: false },
          { combatantId: enemyId, x: 2, y: 2, vitalityDelta: 3, defeated: false, missed: false },
        ],
      })],
      { allies: [], enemies: [{ combatant: { id: enemyId } }] } as never,
      () => 0,
    );

    expect(performance.now() - start).toBeGreaterThanOrEqual(
      TICK_IMPACT_STAGGER_MS + TICK_SETTLE_MS - 20,
    );
  });
});
