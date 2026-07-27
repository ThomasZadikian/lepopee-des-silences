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
 */

/** Avant qu'un ennemi n'entame son tour : il regarde le terrain. // BALANCE KNOB */
export const THINK_MS = 520;

/** Par case franchie. // BALANCE KNOB */
export const STEP_MS = 145;

/** Après le geste d'un ennemi, avant de passer la main. // BALANCE KNOB */
export const SETTLE_MS = 620;

/** Durée de vie d'un chiffre flottant, et hauteur de sa montée. */
export const FLOAT_MS = 1100;
export const FLOAT_RISE_PX = 30;

export type FloatingNumber = {
  id: number;
  x: number;
  y: number;
  text: string;
  /** Rouge quand un allié encaisse, ambre quand c'est l'adversaire — la référence de conception. */
  color: string;
  bornAt: number;
};

/** Le déplacement en cours, s'il y en a un : l'interpolation vit ici, pas dans le rendu. */
export type WalkAnimation = {
  combatantId: string;
  /** Position de départ incluse, pour que le premier pas parte du bon endroit. */
  path: Array<{ x: number; y: number }>;
  startedAt: number;
};

const ALLY_HIT_COLOR = '#ff8f7a';
const ENEMY_HIT_COLOR = '#ffd98a';
const HEAL_COLOR = '#86dcb4';

export function useCombatPlayback() {
  const walk = shallowRef<WalkAnimation | null>(null);
  const floats = ref<FloatingNumber[]>([]);
  const isPlaying = ref(false);

  /**
   * Positions surchargées pendant la lecture.
   *
   * L'état final du serveur place déjà chaque figure à son arrivée. Tant que la chronologie
   * n'est pas jouée, on la ré-épingle à son point de départ : sans cela, la figure serait déjà
   * arrivée avant même que sa marche ne commence.
   */
  const pinned = ref<Record<string, { x: number; y: number }>>({});

  let floatSeq = 0;
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
    isPlaying.value = false;
  }

  function reset() {
    stop();
    floats.value = [];
  }

  /** Efface les chiffres arrivés au bout de leur vie. Appelé depuis la boucle de rendu. */
  function pruneFloats(now: number) {
    if (floats.value.length === 0) return;

    floats.value = floats.value.filter((f) => now - f.bornAt < FLOAT_MS);
  }

  /** La position d'un combattant à cet instant : interpolée s'il marche, épinglée sinon. */
  function positionOf(
    combatantId: string,
    settled: { x: number; y: number },
    now: number,
  ): { x: number; y: number } {
    const current = walk.value;

    if (current && current.combatantId === combatantId && current.path.length > 0) {
      const progress = (now - current.startedAt) / STEP_MS;

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
   */
  async function play(
    events: readonly TacticalCombatEventDto[],
    finalState: TacticalCombatRuntimeDto,
    now: () => number,
  ): Promise<void> {
    if (events.length === 0) return;

    const allyIds = new Set(finalState.allies.map((a) => a.combatant.id));

    isPlaying.value = true;

    try {
      for (const event of events) {
        const actorIsAlly = allyIds.has(event.actorId);

        // L'adversaire prend le temps de décider. Un allié agit sur ordre du joueur : il n'a
        // rien à peser, et le faire attendre passerait pour de la latence.
        if (!actorIsAlly) await wait(THINK_MS);

        if (event.kind === 'Move' && event.path.length > 0) {
          // Le chemin serveur ne contient que les cases foulées, origine exclue. Celle-ci vient
          // du relevé pris avant l'appel ; à défaut, on part de la première case du trajet —
          // une case d'écart, jamais un saut à travers le plateau.
          const origin = pinned.value[event.actorId] ?? event.path[0];

          const path = [
            { x: origin.x, y: origin.y },
            ...event.path.map((s) => ({ x: s.x, y: s.y })),
          ];

          walk.value = { combatantId: event.actorId, path, startedAt: now() };
          await wait(event.path.length * STEP_MS);
          walk.value = null;

          // Arrivé : la figure reste où le serveur l'a mise.
          const { [event.actorId]: _removed, ...rest } = pinned.value;
          pinned.value = rest;
          continue;
        }

        if (event.kind === 'Skill') {
          const at = now();

          for (const impact of event.impacts) {
            pushFloat(impact.x, impact.y, impact.vitalityDelta, allyIds.has(impact.combatantId), at);
          }

          if (!actorIsAlly) await wait(SETTLE_MS);
        }
      }
    } finally {
      walk.value = null;
      pinned.value = {};
      isPlaying.value = false;
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

  return {
    walk,
    floats,
    isPlaying: computed(() => isPlaying.value),
    positionOf,
    pruneFloats,
    play,
    pinBefore,
    reset,
    stop,
  };
}
