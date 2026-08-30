import { describe, expect, it } from 'vitest';

import { COMBAT_DEFAULT_ZOOM, useCombatCamera } from './useCombatCamera';

describe('combat camera', () => {
  it('uses the fixed combat scale 0.55', () => {
    const camera = useCombatCamera();

    expect(COMBAT_DEFAULT_ZOOM).toBe(0.55);
    expect(camera.camera.value.zoom).toBe(0.55);
  });

  it('can focus instantly without changing the combat zoom', async () => {
    const camera = useCombatCamera();

    await camera.focusTo(7, 4, { instant: true });

    expect(camera.camera.value).toEqual({ camX: 7, camY: 4, zoom: 0.55 });
    expect(camera.isAnimating.value).toBe(false);
  });
});
