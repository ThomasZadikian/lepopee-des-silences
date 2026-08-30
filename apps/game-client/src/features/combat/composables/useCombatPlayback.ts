import { computed, ref, shallowRef } from 'vue';

import type {
  CombatantStatusEffectDto,
  TacticalCombatEventDto,
  TacticalCombatRuntimeDto,
} from '../types/combatContracts';

/** Multiplicateur global de rythme. >1 ralentit, <1 accélère. */
export const PACE = 1.25;

export const THINK_MS = Math.round(520 * PACE);
export const BASE_STEP_MS = Math.round(145 * PACE);
export const SETTLE_MS = Math.round(620 * PACE);
export const FLOAT_MS = Math.round(1100 * PACE);
export const FLOAT_RISE_PX = 30;
export const IMPACT_MS = Math.round(800 * PACE);
// Conservé pour compatibilité avec les tests/consommateurs historiques.
// Le fondu de changement de tour a été supprimé : il ne doit plus ajouter de latence visuelle.
export const TURN_TRANSITION_MS = 0;
export const TICK_SETTLE_MS = Math.round(400 * PACE);
export const TICK_IMPACT_STAGGER_MS = Math.round(260 * PACE);
export const TELEGRAPH_MS = Math.round(1200 * PACE);
export const ENEMY_STEP_MULTIPLIER = 1.2;
export const ENEMY_SETTLE_MULTIPLIER = 1.5;

export function dynamicStepDurationMs(pathLength: number, isEnemy = false): number {
  const baseStep = isEnemy
    ? Math.floor(BASE_STEP_MS * ENEMY_STEP_MULTIPLIER)
    : BASE_STEP_MS;

  if (pathLength <= 2) return Math.floor(baseStep * 0.7);
  if (pathLength >= 5) return Math.floor(baseStep * 1.3);
  return baseStep;
}

export type FloatingNumber = {
  id: number;
  x: number;
  y: number;
  text: string;
  color: string;
  bornAt: number;
  kind?: 'damage' | 'guard' | 'effectiveness';
};

export type ImpactEffect = {
  id: number;
  x: number;
  y: number;
  color: string;
  bornAt: number;
};

export type PendingSort = {
  skillKey: string;
  x: number;
  y: number;
  casterX: number;
  casterY: number;
};

/**
 * Ordre cinématique envoyé par la chronologie à la scène.
 * La scène doit résoudre la Promise uniquement lorsque le mouvement de caméra est terminé.
 */
export type CombatCameraCue = {
  kind: 'actor' | 'action';
  actorId: string;
  actorX: number;
  actorY: number;
  targetX?: number;
  targetY?: number;
};

export type WalkAnimation = {
  combatantId: string;
  path: Array<{ x: number; y: number }>;
  startedAt: number;
  isEnemy: boolean;
};

export type Telegraph = {
  kind: 'Move' | 'Skill' | 'Item' | 'Tick';
  cells: Array<{ x: number; y: number }>;
  label: string;
};

type SortAnimator = (sort: PendingSort) => Promise<void>;
type SceneTransitionWaiter = () => Promise<void>;
type CameraAnimator = (cue: CombatCameraCue) => Promise<void>;

const ALLY_HIT_COLOR = '#ff8f7a';
const ENEMY_HIT_COLOR = '#ffd98a';
const HEAL_COLOR = '#86dcb4';
const MISS_COLOR = '#c3c0d6';
const GUARD_COLOR = '#ffcc33';
const WEAKNESS_COLOR = '#ff5c4d';
const RESISTANT_COLOR = '#7fb2e0';
const IMMUNE_COLOR = '#c3c0d6';

const EFFECTIVENESS_LABELS: Record<'Weak' | 'Resistant' | 'Immune', string> = {
  Weak: 'Faiblesse',
  Resistant: 'Résistance',
  Immune: 'Immunisé',
};

const EFFECTIVENESS_COLORS: Record<'Weak' | 'Resistant' | 'Immune', string> = {
  Weak: WEAKNESS_COLOR,
  Resistant: RESISTANT_COLOR,
  Immune: IMMUNE_COLOR,
};

/**
 * Lecteur séquentiel de la chronologie tactique.
 *
 * Principe central : caméra → annonce → déplacement/sort → impacts → settle. Chaque étape est
 * attendue avant la suivante. La seule animation volontairement concomitante est le suivi de
 * caméra pendant une marche, qui appartient au même geste de déplacement et est piloté par la
 * scène via `walk`/`positionOf`.
 */
export function useCombatPlayback() {
  const walk = shallowRef<WalkAnimation | null>(null);
  const floats = ref<FloatingNumber[]>([]);
  const impacts = ref<ImpactEffect[]>([]);
  const isPlaying = ref(false);
  const actionBanner = ref<string | null>(null);
  const pinned = ref<Record<string, { x: number; y: number }>>({});
  const displayVitals = ref<Record<string, number>>({});
  const displayStatusEffects = ref<Record<string, CombatantStatusEffectDto[]>>({});
  const isTransitioning = ref(false);
  const transitionPhase = ref<'fadeOut' | 'fadeIn' | null>(null);
  const telegraph = shallowRef<Telegraph | null>(null);

  let floatSeq = 0;
  let impactSeq = 0;
  let timers: ReturnType<typeof globalThis.setTimeout>[] = [];
  let sortAnimator: SortAnimator | null = null;
  let sceneTransitionWaiter: SceneTransitionWaiter | null = null;
  let cameraAnimator: CameraAnimator | null = null;

  const wait = (ms: number) =>
    new Promise<void>((resolve) => {
      timers.push(globalThis.setTimeout(resolve, ms));
    });

  function stop() {
    timers.forEach(globalThis.clearTimeout);
    timers = [];
    walk.value = null;
    pinned.value = {};
    displayVitals.value = {};
    displayStatusEffects.value = {};
    actionBanner.value = null;
    telegraph.value = null;
    isPlaying.value = false;
    isTransitioning.value = false;
    transitionPhase.value = null;
  }

  function reset() {
    stop();
    floats.value = [];
    impacts.value = [];
  }

  function setSortAnimator(animator: SortAnimator | null) {
    sortAnimator = animator;
  }

  function setSceneTransitionWaiter(waiter: SceneTransitionWaiter | null) {
    sceneTransitionWaiter = waiter;
  }

  function setCameraAnimator(animator: CameraAnimator | null) {
    cameraAnimator = animator;
  }

  function vitalsOf(combatantId: string, settled: number): number {
    const tracked = displayVitals.value[combatantId];
    return tracked === undefined ? settled : tracked;
  }

  function statusEffectsOf(
    combatantId: string,
    settled: CombatantStatusEffectDto[],
  ): CombatantStatusEffectDto[] {
    const tracked = displayStatusEffects.value[combatantId];
    return tracked === undefined ? settled : tracked;
  }

  function pruneFloats(now: number) {
    if (floats.value.length === 0) return;
    floats.value = floats.value.filter((f) => now - f.bornAt < FLOAT_MS);
  }

  function pruneImpacts(now: number) {
    if (impacts.value.length === 0) return;
    impacts.value = impacts.value.filter((i) => now - i.bornAt < IMPACT_MS);
  }

  function pushImpact(x: number, y: number, targetIsAlly: boolean, now: number) {
    impacts.value = [
      ...impacts.value,
      {
        id: (impactSeq += 1),
        x,
        y,
        color: targetIsAlly ? ALLY_HIT_COLOR : ENEMY_HIT_COLOR,
        bornAt: now,
      },
    ];
  }

  function positionOf(
    combatantId: string,
    settled: { x: number; y: number },
    now: number,
  ): { x: number; y: number } {
    const current = walk.value;

    if (current && current.combatantId === combatantId && current.path.length > 0) {
      const stepMs = dynamicStepDurationMs(current.path.length - 1, current.isEnemy);
      const progress = (now - current.startedAt) / stepMs;
      const step = Math.max(0, Math.floor(progress));
      const at = Math.min(current.path.length - 1, step);
      return { x: current.path[at].x, y: current.path[at].y };
    }

    return pinned.value[combatantId] ?? settled;
  }

  function pushFloat(
    x: number,
    y: number,
    delta: number,
    targetIsAlly: boolean,
    now: number,
    missed = false,
  ) {
    const healed = delta < 0;
    floats.value = [
      ...floats.value,
      {
        id: (floatSeq += 1),
        x,
        y,
        text: missed ? 'Manqué' : healed ? `+${Math.abs(delta)}` : `−${delta}`,
        color: missed
          ? MISS_COLOR
          : healed ? HEAL_COLOR : targetIsAlly ? ALLY_HIT_COLOR : ENEMY_HIT_COLOR,
        bornAt: now,
      },
    ];
  }

  function pushGuardFloat(x: number, y: number, amount: number, now: number) {
    floats.value = [
      ...floats.value,
      {
        id: (floatSeq += 1),
        x,
        y,
        text: `−${amount}`,
        color: GUARD_COLOR,
        bornAt: now,
        kind: 'guard',
      },
    ];
  }

  function pushEffectivenessFloat(
    x: number,
    y: number,
    effectiveness: 'Weak' | 'Resistant' | 'Immune',
    now: number,
  ) {
    floats.value = [
      ...floats.value,
      {
        id: (floatSeq += 1),
        x,
        y,
        text: EFFECTIVENESS_LABELS[effectiveness],
        color: EFFECTIVENESS_COLORS[effectiveness],
        bornAt: now,
        kind: 'effectiveness',
      },
    ];
  }

  async function announce(event: TacticalCombatEventDto) {
    const cells = event.telegraphCells ?? [];
    if (cells.length === 0) {
      await wait(THINK_MS);
      return;
    }

    telegraph.value = {
      kind: event.kind,
      cells: cells.map((cell) => ({ x: cell.x, y: cell.y })),
      label: event.kind === 'Move'
        ? `${event.actorName} se déplace`
        : `${event.actorName} prépare « ${event.skillName ?? '…'} »`,
    };

    await wait(TELEGRAPH_MS);
    telegraph.value = null;
  }

  function settledPositionMap(finalState: TacticalCombatRuntimeDto) {
    return new Map(
      [...finalState.allies, ...finalState.enemies]
        .map((unit) => [unit.combatant.id, { x: unit.x, y: unit.y }] as const),
    );
  }

  function actorPosition(
    event: TacticalCombatEventDto,
    settled: ReadonlyMap<string, { x: number; y: number }>,
  ): { x: number; y: number } {
    const pinnedActor = pinned.value[event.actorId];
    if (pinnedActor) return pinnedActor;

    // À l'ouverture du combat il peut ne pas y avoir de pinBefore. Pour un déplacement, la
    // première case du chemin est alors un meilleur point de départ visuel que la position
    // finale déjà présente dans `finalState`.
    if (event.kind === 'Move' && event.path.length > 0) return event.path[0];

    return settled.get(event.actorId)
      ?? event.path[0]
      ?? {
        x: typeof event.targetX === 'number' ? event.targetX : 0,
        y: typeof event.targetY === 'number' ? event.targetY : 0,
      };
  }

  async function cueActor(
    event: TacticalCombatEventDto,
    position: { x: number; y: number },
  ) {
    if (!cameraAnimator || event.kind === 'Tick') return;
    await cameraAnimator({
      kind: 'actor',
      actorId: event.actorId,
      actorX: position.x,
      actorY: position.y,
    });
  }

  async function cueAction(
    event: TacticalCombatEventDto,
    position: { x: number; y: number },
  ) {
    if (!cameraAnimator) return;
    if (typeof event.targetX !== 'number' || typeof event.targetY !== 'number') return;

    await cameraAnimator({
      kind: 'action',
      actorId: event.actorId,
      actorX: position.x,
      actorY: position.y,
      targetX: event.targetX,
      targetY: event.targetY,
    });
  }

  function applyImpact(
    impact: TacticalCombatEventDto['impacts'][number],
    allyIds: ReadonlySet<string>,
    at: number,
  ) {
    const targetIsAlly = allyIds.has(impact.combatantId);
    const guardAbsorbed = impact.guardAbsorbed ?? 0;

    if (guardAbsorbed > 0) pushGuardFloat(impact.x, impact.y, guardAbsorbed, at);
    if (impact.missed || impact.vitalityDelta !== 0) {
      pushFloat(impact.x, impact.y, impact.vitalityDelta, targetIsAlly, at, impact.missed);
    }
    if (impact.effectiveness) {
      pushEffectivenessFloat(impact.x, impact.y, impact.effectiveness, at);
    }

    if (!impact.missed) {
      displayVitals.value = {
        ...displayVitals.value,
        [impact.combatantId]:
          (displayVitals.value[impact.combatantId] ?? 0) - impact.vitalityDelta,
      };
      pushImpact(impact.x, impact.y, targetIsAlly, at);
    }
  }

  async function play(
    events: readonly TacticalCombatEventDto[],
    finalState: TacticalCombatRuntimeDto,
    now: () => number,
  ): Promise<void> {
    if (events.length === 0) {
      displayStatusEffects.value = {};
      return;
    }

    const allyIds = new Set<string>(finalState.allies.map((a) => a.combatant.id));
    const settledPositions = settledPositionMap(finalState);
    let previousActorId: string | null = null;

    const startingVitals: Record<string, number> = {};
    for (const combatant of [...finalState.allies, ...finalState.enemies]) {
      startingVitals[combatant.combatant.id] = combatant.combatant.currentVitality;
    }
    for (const event of events) {
      for (const impact of event.impacts) {
        if (impact.missed) continue;
        startingVitals[impact.combatantId] =
          (startingVitals[impact.combatantId] ?? 0) + impact.vitalityDelta;
      }
    }
    displayVitals.value = startingVitals;

    isPlaying.value = true;

    try {
      // Le HUD finit d'abord sa transition. La caméra ne commence qu'ensuite.
      if (sceneTransitionWaiter) await sceneTransitionWaiter();

      for (const event of events) {
        const actorIsAlly = allyIds.has(event.actorId);
        const actorChanged = event.actorId !== previousActorId;
        const actorPos = actorPosition(event, settledPositions);

        // Un même tour peut être découpé en plusieurs événements (Move puis Skill/Item).
        // La caméra ne doit pas rejouer un recentrage sur le même acteur entre ces deux morceaux :
        // elle continue depuis la position atteinte pendant la marche. On ne recentre donc
        // l'acteur qu'au premier événement de sa séquence.
        if (actorChanged) await cueActor(event, actorPos);

        // Pour un geste adverse, la caméra cadre l'acteur + sa cible AVANT le télégraphe : la
        // zone annoncée doit forcément être visible pendant son temps de lecture. Le joueur,
        // lui, n'a pas besoin de télégraphe sur l'ordre qu'il vient de donner ; son cadrage
        // d'action se fera juste avant le FX plus bas.
        if (!actorIsAlly && (event.kind === 'Skill' || event.kind === 'Item')) {
          await cueAction(event, actorPos);
        }
        if (!actorIsAlly && event.kind !== 'Tick') await announce(event);
        previousActorId = event.actorId;

        if (event.kind === 'Tick') {
          for (let i = 0; i < event.impacts.length; i += 1) {
            const impact = event.impacts[i];
            applyImpact(impact, allyIds, now());
            if (i < event.impacts.length - 1) await wait(TICK_IMPACT_STAGGER_MS);
          }
          await wait(Math.max(TICK_SETTLE_MS, FLOAT_MS, IMPACT_MS));
          continue;
        }

        if (event.kind === 'Move' && event.path.length > 0) {
          const pinnedOrigin = pinned.value[event.actorId];
          const path = pinnedOrigin
            ? [
                { x: pinnedOrigin.x, y: pinnedOrigin.y },
                ...event.path.map((step) => ({ x: step.x, y: step.y })),
              ]
            : event.path.map((step) => ({ x: step.x, y: step.y }));

          const stepMs = dynamicStepDurationMs(path.length - 1, !actorIsAlly);
          walk.value = {
            combatantId: event.actorId,
            path,
            startedAt: now(),
            isEnemy: !actorIsAlly,
          };

          // La scène suit `walk` pendant cette attente. Ce suivi caméra et la marche sont un
          // même geste ; l'événement suivant ne commence qu'une fois les deux arrivés.
          await wait((path.length - 1) * stepMs);
          walk.value = null;

          const { [event.actorId]: _removed, ...rest } = pinned.value;
          pinned.value = rest;
          continue;
        }

        if (event.kind === 'Skill' || event.kind === 'Item') {
          actionBanner.value = event.skillName
            ? `${event.actorName} — ${event.skillName}`
            : event.actorName;

          // Pour le joueur, la cible n'a pas encore été cadrée (pas de télégraphe allié) :
          // on place donc la caméra entre acteur et cible maintenant. Pour l'adversaire, ce
          // cadrage a déjà été attendu avant le télégraphe et ne doit surtout pas être rejoué.
          if (actorIsAlly) await cueAction(event, actorPos);

          // 1) animation du sort, seule ;
          // 2) impacts/chiffres ; leur temps de lisibilité englobe le settle ;
          // puis seulement l'événement suivant. Aucun Promise.all avec le sort : le chevauchement était
          // précisément la source des effets qui partaient pendant un dézoom/recentrage.
          if (
            event.kind === 'Skill'
              && event.skillKey
              && typeof event.targetX === 'number'
              && typeof event.targetY === 'number'
              && sortAnimator
          ) {
            await sortAnimator({
              skillKey: event.skillKey,
              x: event.targetX,
              y: event.targetY,
              casterX: actorPos.x,
              casterY: actorPos.y,
            });
          }

          const settleMs = actorIsAlly ? SETTLE_MS : SETTLE_MS * ENEMY_SETTLE_MULTIPLIER;
          if (event.impacts.length > 0) {
            const at = now();
            for (const impact of event.impacts) applyImpact(impact, allyIds, at);
            // Le settle n'est pas une animation distincte : c'est le temps minimal pendant
            // lequel on laisse l'impact se lire. On attend donc la plus longue durée, sans
            // rallonger artificiellement la séquence après la disparition des FX.
            await wait(Math.max(FLOAT_MS, IMPACT_MS, settleMs));
          } else {
            await wait(settleMs);
          }
          actionBanner.value = null;
        }
      }
    } finally {
      walk.value = null;
      pinned.value = {};
      displayStatusEffects.value = {};
      actionBanner.value = null;
      telegraph.value = null;
      isPlaying.value = false;
      isTransitioning.value = false;
      transitionPhase.value = null;
    }
  }

  function pinBefore(state: TacticalCombatRuntimeDto | null) {
    if (!state) return;

    const pins: Record<string, { x: number; y: number }> = {};
    const statusPins: Record<string, CombatantStatusEffectDto[]> = {};
    for (const unit of [...state.allies, ...state.enemies]) {
      pins[unit.combatant.id] = { x: unit.x, y: unit.y };
      statusPins[unit.combatant.id] = unit.combatant.statusEffects ?? [];
    }

    pinned.value = pins;
    displayStatusEffects.value = statusPins;
  }

  return {
    walk,
    floats,
    impacts,
    actionBanner,
    telegraph,
    isPlaying: computed(() => isPlaying.value),
    isTransitioning: computed(() => isTransitioning.value),
    transitionPhase: computed(() => transitionPhase.value),
    positionOf,
    vitalsOf,
    statusEffectsOf,
    pruneFloats,
    pruneImpacts,
    setSortAnimator,
    setSceneTransitionWaiter,
    setCameraAnimator,
    play,
    pinBefore,
    reset,
    stop,
  };
}
