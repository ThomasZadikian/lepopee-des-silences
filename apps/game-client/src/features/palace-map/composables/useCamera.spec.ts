import { ref } from 'vue';
import { describe, expect, it } from 'vitest';
import { useCamera } from './useCamera';
import { DEFAULT_ZOOM } from './useTerrainDrawPlan';

describe('useCamera', () => {
  it('centers on the party position it is given', () => {
    const x = ref(3);
    const y = ref(4);
    const { camera } = useCamera(x, y);

    expect(camera.value).toEqual({ camX: 3, camY: 4, zoom: DEFAULT_ZOOM });
  });

  it('tracks the party refs reactively as they animate', () => {
    const x = ref(0);
    const y = ref(0);
    const { camera } = useCamera(x, y);

    x.value = 5.5; // usePartyTokenPath animates fractionally mid-step
    y.value = 2.25;

    expect(camera.value).toEqual({ camX: 5.5, camY: 2.25, zoom: DEFAULT_ZOOM });
  });
});
