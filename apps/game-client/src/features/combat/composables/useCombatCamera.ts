import { computed, ref, type ComputedRef } from 'vue';

import type { CameraParams } from '../../palace-map/composables/useTerrainDrawPlan';

const COMBAT_DEFAULT_ZOOM = 0.55;

export function useCombatCamera(): {
  camera: ComputedRef<CameraParams>;
  focus: (x: number, y: number) => void;
  setZoom: (zoom: number) => void;
} {
  const camX = ref(0);
  const camY = ref(0);
  const zoom = ref(COMBAT_DEFAULT_ZOOM);

  const camera = computed<CameraParams>(() => ({
    camX: camX.value,
    camY: camY.value,
    zoom: zoom.value,
  }));

  function focus(x: number, y: number): void {
    camX.value = x;
    camY.value = y;
  }

  function setZoom(value: number): void {
    zoom.value = Math.max(0.35, Math.min(1.25, value));
  }

  return {
    camera,
    focus,
    setZoom,
  };
}