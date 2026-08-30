import { computed, type ComputedRef, type Ref } from 'vue';

import { DEFAULT_ZOOM, type CameraParams } from './useTerrainDrawPlan';

export const EXPLORATION_ZOOM = 0.45;

/**
 * The exploration map's camera: centered on the party's own animated grid position
 * (usePartyTokenPath's displayPartyX/displayPartyY, the same refs the step animation already
 * drives) at a fixed zoom. No timer or watcher of its own — the camera glides exactly when and
 * as fast as the party token does, for free.
 *
 * Manual pan/zoom are deferred (see the exploration-camera plan): this is intentionally the
 * simplest thing that makes a room bigger than the viewport still feel like a real place to
 * walk through, not a control surface yet.
 */
export function useCamera(
  displayPartyX: Ref<number>,
  displayPartyY: Ref<number>,
): { camera: ComputedRef<CameraParams> } {
  const camera = computed<CameraParams>(() => ({
    camX: displayPartyX.value,
    camY: displayPartyY.value,
    zoom: DEFAULT_ZOOM * EXPLORATION_ZOOM,
  }));

  return { camera };
}
