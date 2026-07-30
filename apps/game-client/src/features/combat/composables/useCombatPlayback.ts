import { computed, ref, shallowRef } from 'vue';

import type {
  TacticalCombatEventDto,
  TacticalCombatRuntimeDto,
} from '../types/combatContracts';

/**
 * Le rythme du combat tactique.
 *
 * Le serveur résout un tour entier d'un coup ; sans mise en scène, le joueur verrait des
 * figures se téléporter et des dégâts surgir sans cause. Ce lecteur déroule la chronologie
 * renvoyée à côté de l'état final, en respectant les temps de la référence de conception.
 *
 * Aucune de ces durées n'est cosmétique. Le temps de réflexion donne à l'adversaire l'air de
 * décider plutôt que de réagir ; le pas de marche rend le trajet lisible, en particulier quand
 * il contourne un mur ; la pause finale laisse le coup se déposer avant que la main revienne.
 *
 * Optimisé pour O-014 : timings dynamiques en fonction de la distance.
 * Optimisé pour O-017 : transitions entre les tours (fondu).
 * Optimisé pour O-018 : timings spécifiques pour les ennemis.
 */

/** Avant qu'un ennemi n'entame son tour : il regarde le terrain. // BALANCE KNOB */
export const THINK_MS = 520;

/** Par case franchie (base). // BALANCE KNOB */
export const BASE_STEP_MS = 145;

/** Après le geste d'un ennemi, avant de passer la main. // BALANCE KNOB */
export const SETTLE_MS = 620;

/** Durée de vie d'un chiffre flottant, et hauteur de sa montée. */
export const FLOAT_MS = 1100;
export const FLOAT_RISE_PX = 30;

/** Durée de la transition entre les tours (fondu). // O-017 */
export const TURN_TRANSITION_MS = 300;

/** Multiplicateur de timing pour les ennemis (O-018). */
export const ENEMY_STEP_MULTIPLIER = 1.2; // 20% plus lent
/** Multiplicateur de timing pour le settle des ennemis (O-018). */
export const ENEMY_SETTLE_MULTIPLIER = 1.5; // 50% plus long

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

export type WalkAnimation = {
  combatantId: string;
  path: Array<{ x: number; y: number }>;
  startedAt: number;
  isEnemy: boolean;
};

const ALLY_HIT_COLOR = '#ff8f7a';
const ENEMY_HIT_COLOR = '#ffd98a';
const HEAL_COLOR = '#86dcb4';

export function useCombatPlayback() {
  const walk = shallowRef<WalkAnimation | null>(null);
  const floats = ref<FloatingNumber[]>([]);
  const impacts = ref<ImpactEffect[]>([]);
  const pendingSorts = ref<PendingSort[]>([]);
  const isPlaying = ref(false);

  const actionBanner = ref<string | null>(null);

  const pinned = ref<Record<string, { x: number; y: number }>>({});

  // O-017: État de transition entre les tours
  const isTransitioning = ref(false);
  const transitionPhase = ref<'fadeOut' | 'fadeIn' | null>(null);

  let floatSeq = 0;
  let impactSeq = 0;
  let timers: ReturnType<typeof globalThis.setTimeout>[] = [];

  const wait = (ms: number) =>
    new Promise<void>((resolve) => {
      timers.push(globalThis.setTimeout(resolve, ms));
    });

  function stop() {
    timers.forEach(globalThis.clearTimeout);
    timers = [];
    walk.value = null;
    pinned.value = {};
    actionBanner.value = null;
    pendingSorts.value = [];
    isPlaying.value = false;
    // O-017: Réinitialiser l'état de transition
    isTransitioning.value = false;
    transitionPhase.value = null;
  }

  function reset() {
    stop();
    floats.value = [];
    impacts.value = [];
  }

  function pruneFloats(now: number) {
    if (floats.value.length === 0) return;

    floats.value = floats.value.filter((f) => now - f.bornAt < FLOAT_MS);
  }

  const IMPACT_MS = 800;

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

  /**
   * Calcule le timing dynamique pour un pas de déplacement (O-014 + O-018).
   * - Déplacements courts (1-2 cases) : plus rapides (70% de BASE_STEP_MS).
   * - Déplacements longs (5+ cases) : plus lents (130% de BASE_STEP_MS).
   * - Ennemis : 20% plus lents (ENEMY_STEP_MULTIPLIER).
   */
  /** La position d'un combattant à cet instant : interpolée s'il marche, épinglée sinon. */
  function positionOf(
    combatantId: string,
    settled: { x: number; y: number },
    now: number,
  ): { x: number; y: number } {
    const current = walk.value;

    if (current && current.combatantId === combatantId && current.path.length > 0) {
      const stepMs = dynamicStepDurationMs(current.path.length - 1, current.isEnemy);
      const progress = (now - current.startedAt) / stepMs;

      // `now` vient de l'horodatage de `requestAnimationFrame`, qui date du DÉBUT de la frame
      // et peut donc précéder le `performance.now()` relevé au lancement de la marche. Sans ce
      // plancher, la progression passe négative, l'index aussi, et la lecture d'une case
      // inexistante fait tomber toute la boucle de rendu.
      const step = Math.max(0, Math.floor(progress));
      const from = Math.min(current.path.length - 1, step);
      const to = Math.min(current.path.length - 1, from + 1);
      const fraction = Math.max(0, Math.min(1, progress - step));

      const a = current.path[from];
      const b = current.path[to];

      return {
        x: a.x + (b.x - a.x) * fraction,
        y: a.y + (b.y - a.y) * fraction,
      };
    }

    return pinned.value[combatantId] ?? settled;
  }

  function pushFloat(
    x: number, y: number, delta: number, targetIsAlly: boolean, now: number,
  ) {
    // Un soin monte en vert avec un signe explicite : « +8 » et « −8 » ne doivent jamais se
    // confondre d'un coup d'œil.
    const healed = delta < 0;

    floats.value = [
      ...floats.value,
      {
        id: (floatSeq += 1),
        x,
        y,
        text: healed ? `+${Math.abs(delta)}` : `−${delta}`,
        color: healed ? HEAL_COLOR : targetIsAlly ? ALLY_HIT_COLOR : ENEMY_HIT_COLOR,
        bornAt: now,
      },
    ];
  }

  /**
   * Déroule une chronologie. Résout quand tout est joué — l'appelant peut alors rendre la main
   * au joueur en sachant que plus rien ne bouge.
   *
   * Optimisé pour O-014 : utilise des timings dynamiques pour les déplacements.
   * Optimisé pour O-017 : ajoute des transitions entre les tours.
   * Optimisé pour O-018 : timings spécifiques pour les ennemis.
   */
  async function play(
    events: readonly TacticalCombatEventDto[],
    finalState: TacticalCombatRuntimeDto,
    now: () => number,
  ): Promise<void> {
    if (events.length === 0) return;

    const allyIds = new Set(finalState.allies.map((a) => a.combatant.id));
    let previousActorId: string | null = null;

    isPlaying.value = true;

    try {
      for (const event of events) {
        const actorIsAlly = allyIds.has(event.actorId);

        const beginsEnemyTurn = !actorIsAlly && event.actorId !== previousActorId;

        // Un déplacement et la compétence qui le suit appartiennent au même tour : la
        // transition et le temps de réflexion ne se jouent qu'une fois pour cette paire.
        if (beginsEnemyTurn) {
          isTransitioning.value = true;
          transitionPhase.value = 'fadeOut';
          await wait(TURN_TRANSITION_MS / 2);
          transitionPhase.value = 'fadeIn';
          await wait(TURN_TRANSITION_MS / 2);
          isTransitioning.value = false;
          transitionPhase.value = null;
        }

        // L'adversaire prend le temps de décider. Un allié agit sur ordre du joueur : il n'a
        // rien à peser, et le faire attendre passerait pour de la latence.
        if (beginsEnemyTurn) await wait(THINK_MS);
        previousActorId = event.actorId;

        if (event.kind === 'Move' && event.path.length > 0) {
          // Le chemin serveur ne contient que les cases foulées, origine exclue. Celle-ci vient
          // du relevé pris avant l'appel ; à défaut, on part de la première case du trajet —
          // une case d'écart, jamais un saut à travers le plateau.
          const origin = pinned.value[event.actorId] ?? event.path[0];

          const path = [
            { x: origin.x, y: origin.y },
            ...event.path.map((s) => ({ x: s.x, y: s.y })),
          ];

          // O-014 + O-018: Utiliser un timing dynamique basé sur la longueur du chemin et le type (allié/ennemi)
          const stepMs = dynamicStepDurationMs(path.length - 1, !actorIsAlly);
          walk.value = {
            combatantId: event.actorId,
            path,
            startedAt: now(),
            isEnemy: !actorIsAlly,
          };
          await wait((path.length - 1) * stepMs); // -1 car origin n'est pas comptée dans le chemin
          walk.value = null;

          // Arrivé : la figure reste où le serveur l'a mise.
          const { [event.actorId]: _removed, ...rest } = pinned.value;
          pinned.value = rest;
          continue;
        }

        if (event.kind === 'Skill' || event.kind === 'Item') {
          const at = now();

          actionBanner.value = event.skillName
            ? `${event.actorName} — ${event.skillName}`
            : event.actorName;

          const actorPos = pinned.value[event.actorId] ?? event.path[0] ?? null;

          for (const impact of event.impacts) {
            pushFloat(impact.x, impact.y, impact.vitalityDelta, allyIds.has(impact.combatantId), at);
            pushImpact(impact.x, impact.y, allyIds.has(impact.combatantId), at);
          }

          if (
            event.kind === 'Skill'
              &&
            event.skillKey
              && typeof event.targetX === 'number'
              && typeof event.targetY === 'number'
          ) {
            pendingSorts.value = [
              ...pendingSorts.value,
              {
                skillKey: event.skillKey,
                x: event.targetX,
                y: event.targetY,
                casterX: actorPos?.x ?? event.targetX,
                casterY: actorPos?.y ?? event.targetY,
              },
            ];
          }

          // O-018: Temps de settle plus long pour les ennemis
          const settleMs = actorIsAlly ? SETTLE_MS / 2 : SETTLE_MS * ENEMY_SETTLE_MULTIPLIER;
          await wait(settleMs);
          actionBanner.value = null;
        }
      }
    } finally {
      walk.value = null;
      pinned.value = {};
      actionBanner.value = null;
      isPlaying.value = false;
      // O-017: Réinitialiser l'état de transition
      isTransitioning.value = false;
      transitionPhase.value = null;
    }
  }

  /** Épingle les positions de départ AVANT que le nouvel état ne soit appliqué. */
  function pinBefore(state: TacticalCombatRuntimeDto | null) {
    if (!state) return;

    const pins: Record<string, { x: number; y: number }> = {};
    for (const unit of [...state.allies, ...state.enemies]) {
      pins[unit.combatant.id] = { x: unit.x, y: unit.y };
    }

    pinned.value = pins;
  }

  function consumeSorts(): PendingSort[] {
    const sorts = pendingSorts.value;
    pendingSorts.value = [];
    return sorts;
  }

  return {
    walk,
    floats,
    impacts,
    pendingSorts,
    actionBanner,
    isPlaying: computed(() => isPlaying.value),
    isTransitioning: computed(() => isTransitioning.value), // O-017
    transitionPhase: computed(() => transitionPhase.value), // O-017
    positionOf,
    pruneFloats,
    pruneImpacts,
    consumeSorts,
    play,
    pinBefore,
    reset,
    stop,
  };
}
