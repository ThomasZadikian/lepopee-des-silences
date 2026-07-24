import { onBeforeUnmount, ref, watch, type ComputedRef } from 'vue';
import type { RoomDto, RoomGridDto } from '../../runs/types/runTypes';

/** Matches the CSS transition duration on .tgrid__party (one cell-to-cell glide per step). */
export const PARTY_STEP_MS = 150;

/**
 * Party token animation: step through the path cell-by-cell instead of a single CSS
 * glide straight from the old grid.partyX/Y to the new one, which cut a diagonal
 * shortcut through untraveled ground and read as a teleport for any move longer than
 * one cell. Mirrors RoomGrid.MoveTo's own path (X axis first, then Y) so the token
 * visibly walks the same cells the domain actually moved it through.
 */
export function usePartyTokenPath(
  room: ComputedRef<RoomDto>,
  grid: ComputedRef<RoomGridDto | null>,
) {
  const prefersReducedMotion =
    typeof window !== 'undefined' && typeof window.matchMedia === 'function'
      ? window.matchMedia('(prefers-reduced-motion: reduce)').matches
      : false;

  const displayPartyX = ref(grid.value?.partyX ?? 0);
  const displayPartyY = ref(grid.value?.partyY ?? 0);
  let partyAnimationTimer: ReturnType<typeof setInterval> | null = null;
  let lastAnimatedRoomId: string | null = null;

  function stopPartyAnimation() {
    if (partyAnimationTimer !== null) {
      clearInterval(partyAnimationTimer);
      partyAnimationTimer = null;
    }
  }

  function snapPartyTo(x: number, y: number) {
    stopPartyAnimation();
    displayPartyX.value = x;
    displayPartyY.value = y;
  }

  function animatePartyTo(targetX: number, targetY: number) {
    stopPartyAnimation();

    const steps: Array<[number, number]> = [];
    let x = displayPartyX.value;
    let y = displayPartyY.value;
    const stepX = Math.sign(targetX - x);
    while (x !== targetX) {
      x += stepX;
      steps.push([x, y]);
    }
    const stepY = Math.sign(targetY - y);
    while (y !== targetY) {
      y += stepY;
      steps.push([x, y]);
    }
    if (steps.length === 0) return;

    let stepIndex = 0;
    partyAnimationTimer = setInterval(() => {
      const step = steps[stepIndex];
      if (!step) {
        stopPartyAnimation();
        return;
      }
      [displayPartyX.value, displayPartyY.value] = step;
      stepIndex += 1;
      if (stepIndex >= steps.length) stopPartyAnimation();
    }, PARTY_STEP_MS);
  }

  watch(
    () => (grid.value ? ([room.value.id, grid.value.partyX, grid.value.partyY] as const) : null),
    (next) => {
      if (!next) return;
      const [roomId, x, y] = next;
      const isNewRoom = roomId !== lastAnimatedRoomId;
      lastAnimatedRoomId = roomId;
      if (isNewRoom || prefersReducedMotion) {
        snapPartyTo(x, y);
      } else {
        animatePartyTo(x, y);
      }
    },
    { immediate: true },
  );

  onBeforeUnmount(() => stopPartyAnimation());

  return {
    prefersReducedMotion,
    displayPartyX,
    displayPartyY,
    animatePartyTo,
    snapPartyTo,
    stopPartyAnimation,
  };
}
