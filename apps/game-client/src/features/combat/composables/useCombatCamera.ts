import { computed, ref, type ComputedRef } from 'vue';

import type { CameraParams } from '../../palace-map/composables/useTerrainDrawPlan';

/**
 * Échelle visuelle du combat. Contrairement à l'ancien fit-to-grid, elle ne dépend jamais de
 * la taille de l'arène : une carte 12×12 et une carte 50×50 gardent des cases de même taille.
 */
export const COMBAT_DEFAULT_ZOOM = 0.55;

/** Durée nominale d'un recentrage court. La distance allonge légèrement ce temps. */
const CAMERA_FOCUS_BASE_MS = 260;
const CAMERA_FOCUS_MAX_MS = 620;
const CAMERA_FOCUS_MS_PER_CELL = 34;

/** Vitesse de rattrapage pendant qu'une unité marche, en unités exponentielles/seconde. */
const CAMERA_FOLLOW_SPEED = 9;

export type CombatCameraFocusOptions = {
  instant?: boolean;
  durationMs?: number;
};

export type CombatCameraController = {
  camera: ComputedRef<CameraParams>;
  isAnimating: ComputedRef<boolean>;
  jumpTo: (x: number, y: number) => void;
  focusTo: (x: number, y: number, options?: CombatCameraFocusOptions) => Promise<void>;
  follow: (x: number, y: number, deltaSeconds: number) => void;
  cancel: () => void;
};

function easeInOutCubic(t: number): number {
  return t < 0.5
    ? 4 * t * t * t
    : 1 - (Math.pow(-2 * t + 2, 3) / 2);
}

function focusDuration(distance: number): number {
  return Math.min(
    CAMERA_FOCUS_MAX_MS,
    CAMERA_FOCUS_BASE_MS + (distance * CAMERA_FOCUS_MS_PER_CELL),
  );
}

/**
 * Caméra dédiée au combat tactique.
 *
 * Deux mouvements sont distingués :
 * - `focusTo` est une animation bloquante, utilisée entre deux séquences de combat. Son
 *   Promise ne se résout qu'une fois la caméra arrivée : un sort ne peut donc pas démarrer
 *   pendant un recentrage ;
 * - `follow` accompagne la marche déjà en cours. Ce suivi fait partie de la même animation de
 *   déplacement et n'introduit pas une seconde séquence indépendante.
 */
export function useCombatCamera(): CombatCameraController {
  const camX = ref(0);
  const camY = ref(0);
  const animating = ref(false);

  let frameHandle = 0;
  let animationToken = 0;
  let pendingResolve: (() => void) | null = null;

  const camera = computed<CameraParams>(() => ({
    camX: camX.value,
    camY: camY.value,
    zoom: COMBAT_DEFAULT_ZOOM,
  }));

  function finishPendingAnimation(): void {
    if (frameHandle) {
      globalThis.cancelAnimationFrame(frameHandle);
      frameHandle = 0;
    }
    animating.value = false;
    const resolve = pendingResolve;
    pendingResolve = null;
    resolve?.();
  }

  function cancel(): void {
    animationToken += 1;
    finishPendingAnimation();
  }

  function jumpTo(x: number, y: number): void {
    cancel();
    camX.value = x;
    camY.value = y;
  }

  function focusTo(
    x: number,
    y: number,
    options: CombatCameraFocusOptions = {},
  ): Promise<void> {
    const startX = camX.value;
    const startY = camY.value;
    const distance = Math.hypot(x - startX, y - startY);

    if (options.instant || distance < 0.001) {
      jumpTo(x, y);
      return Promise.resolve();
    }

    cancel();

    const token = ++animationToken;
    const duration = Math.max(1, options.durationMs ?? focusDuration(distance));
    const startedAt = performance.now();
    animating.value = true;

    return new Promise<void>((resolve) => {
      pendingResolve = resolve;

      const step = (timestamp: number) => {
        if (token !== animationToken) return;

        const progress = Math.min(1, Math.max(0, (timestamp - startedAt) / duration));
        const eased = easeInOutCubic(progress);
        camX.value = startX + ((x - startX) * eased);
        camY.value = startY + ((y - startY) * eased);

        if (progress >= 1) {
          camX.value = x;
          camY.value = y;
          frameHandle = 0;
          animating.value = false;
          pendingResolve = null;
          resolve();
          return;
        }

        frameHandle = globalThis.requestAnimationFrame(step);
      };

      frameHandle = globalThis.requestAnimationFrame(step);
    });
  }

  function follow(x: number, y: number, deltaSeconds: number): void {
    if (animating.value) return;

    const dt = Math.max(0, Math.min(0.1, deltaSeconds));
    const t = 1 - Math.exp(-CAMERA_FOLLOW_SPEED * dt);
    camX.value += (x - camX.value) * t;
    camY.value += (y - camY.value) * t;
  }

  return {
    camera,
    isAnimating: computed(() => animating.value),
    jumpTo,
    focusTo,
    follow,
    cancel,
  };
}
