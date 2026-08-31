import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  playSort: vi.fn(),
  shapeCells: vi.fn(),
  isoUnit: vi.fn(() => ({ isoUnitX: 40, isoUnitY: 20 })),
  cameraTileUnit: vi.fn(() => ({ isoUnitX: 60, isoUnitY: 30 })),
  projectToScreen: vi.fn((x: number, y: number) => ({ screenX: x * 10, screenY: y * 10 })),
  projectToScreenCamera: vi.fn((x: number, y: number) => ({ screenX: 100 + x * 10, screenY: 200 + y * 10 })),
}));

vi.mock('../../palace-map/composables/sorts', () => ({
  SORTS: {
    known: { shape: 'Cross' },
    whole: { shape: 'Map' },
  },
  playSort: mocks.playSort,
  shapeCells: mocks.shapeCells,
}));

vi.mock('../../palace-map/composables/useTerrainDrawPlan', () => ({
  isoUnit: mocks.isoUnit,
  cameraTileUnit: mocks.cameraTileUnit,
  projectToScreen: mocks.projectToScreen,
  projectToScreenCamera: mocks.projectToScreenCamera,
}));

vi.mock('./useCombatPlayback', () => ({
  PACE: 1,
}));

import { useSortEffects } from './useSortEffects';

const legacyProjection = {
  canvasWidth: 800,
  canvasHeight: 600,
  gridWidth: 3,
  gridHeight: 2,
};

const cameraProjection = {
  ...legacyProjection,
  camX: 1,
  camY: 2,
  zoom: 1.25,
};

describe('useSortEffects coverage margin', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.clearAllMocks();
    mocks.isoUnit.mockReturnValue({ isoUnitX: 40, isoUnitY: 20 });
    mocks.cameraTileUnit.mockReturnValue({ isoUnitX: 60, isoUnitY: 30 });
    mocks.projectToScreen.mockImplementation((x, y) => ({ screenX: x * 10, screenY: y * 10 }));
    mocks.projectToScreenCamera.mockImplementation((x, y) => ({ screenX: 100 + x * 10, screenY: 200 + y * 10 }));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('ignores missing and unknown sort identifiers', async () => {
    const effects = useSortEffects();

    await effects.launchSort('', 0, 0, { width: 1, height: 1, elevation: [0] }, legacyProjection);
    await effects.launchSort('missing', 0, 0, { width: 1, height: 1, elevation: [0] }, legacyProjection);

    expect(effects.activeSorts.value).toHaveLength(0);
    expect(mocks.shapeCells).not.toHaveBeenCalled();
  });

  it('uses shaped cells, clips out-of-bounds cells and derives distance from the caster', async () => {
    mocks.shapeCells.mockReturnValue([
      { x: 1, y: 0, d: 0 },
      { x: 2, y: 1, d: 2 },
      { x: -1, y: 0, d: 1 },
      { x: 3, y: 0, d: 1 },
      { x: 0, y: -1, d: 1 },
      { x: 0, y: 2, d: 1 },
    ]);
    const effects = useSortEffects();

    const running = effects.launchSort(
      'known',
      1,
      0,
      { width: 3, height: 2, elevation: [0, 1, 0, 0, 0, 2] },
      legacyProjection,
      0,
      0,
      'DIAMOND',
      '#fff',
    );

    const effect = effects.activeSorts.value[0]!;
    expect(mocks.shapeCells).toHaveBeenCalledWith('diamond', 1, 0);
    expect(effect.worldCells).toEqual([
      expect.objectContaining({ x: 1, y: 0, center: true, dist: 1 }),
      expect.objectContaining({ x: 2, y: 1, center: false, dist: 3 }),
    ]);
    expect(effect.fromWorld).toEqual({ x: 0, y: 0 });
    expect(effect.from).toEqual({ x: 0, y: 0 });
    expect(effect.color).toBe('#fff');
    expect(mocks.isoUnit).toHaveBeenCalled();
    expect(mocks.projectToScreen).toHaveBeenCalled();

    await vi.runAllTimersAsync();
    await running;
    expect(effects.activeSorts.value).toHaveLength(0);
  });

  it('falls back to the whole floor and includes all cells when floor metadata is absent', async () => {
    mocks.shapeCells.mockReturnValue(null);
    const effects = useSortEffects();

    const running = effects.launchSort(
      'whole',
      0,
      0,
      { width: 2, height: 2, elevation: [] },
      legacyProjection,
    );

    const effect = effects.activeSorts.value[0]!;
    expect(effect.worldCells).toHaveLength(4);
    expect(effect.worldCells.every((cell) => cell.center && cell.dist === 0)).toBe(true);
    expect(effect.fromWorld).toBeNull();
    expect(effect.from).toBeNull();

    effects.clearSorts();
    await running;
    expect(effects.activeSorts.value).toHaveLength(0);
  });

  it('filters fallback cells with the floor mask', async () => {
    mocks.shapeCells.mockReturnValue(null);
    const effects = useSortEffects();

    const running = effects.launchSort(
      'whole',
      0,
      0,
      { width: 2, height: 2, elevation: [], floor: [true, false, false, true] },
      legacyProjection,
    );

    expect(effects.activeSorts.value[0]!.worldCells.map((cell) => [cell.x, cell.y])).toEqual([
      [0, 0],
      [1, 1],
    ]);

    effects.reset();
    await running;
  });

  it('uses the camera projection only when every camera coordinate is finite', async () => {
    mocks.shapeCells.mockReturnValue([{ x: 0, y: 0, d: 0 }]);
    const effects = useSortEffects();

    const cameraRunning = effects.launchSort(
      'known',
      0,
      0,
      { width: 1, height: 1, elevation: [0] },
      cameraProjection,
      0,
      0,
    );
    expect(mocks.cameraTileUnit).toHaveBeenCalled();
    expect(mocks.projectToScreenCamera).toHaveBeenCalled();

    const invalidProjections = [
      { ...cameraProjection, camX: undefined },
      { ...cameraProjection, camX: Number.NaN },
      { ...cameraProjection, camY: undefined },
      { ...cameraProjection, camY: Number.POSITIVE_INFINITY },
      { ...cameraProjection, zoom: undefined },
      { ...cameraProjection, zoom: Number.NaN },
    ] as any[];

    for (const projection of invalidProjections) {
      await effects.launchSort(
        'known',
        0,
        0,
        { width: 1, height: 1, elevation: [0] },
        projection,
      );
    }

    expect(mocks.isoUnit).toHaveBeenCalled();
    expect(mocks.projectToScreen).toHaveBeenCalled();

    effects.clearSorts();
    await cameraRunning;
  });

  it('reprojects active effects during rendering and supports the stored projection fallback', async () => {
    mocks.shapeCells.mockReturnValue([{ x: 1, y: 0, d: 0 }]);
    const effects = useSortEffects();
    const running = effects.launchSort(
      'known',
      1,
      0,
      { width: 2, height: 1, elevation: [0, 1] },
      legacyProjection,
      0,
      0,
    );
    const ctx = {} as CanvasRenderingContext2D;

    effects.renderSorts(ctx);
    effects.renderSorts(ctx, cameraProjection);

    expect(mocks.playSort).toHaveBeenCalledTimes(2);
    expect(mocks.playSort.mock.calls[0]![4]).toEqual({ x: 0, y: 0 });
    expect(mocks.playSort.mock.calls[1]![4]).toEqual({ x: 100, y: 200 });
    expect(mocks.projectToScreenCamera).toHaveBeenCalled();

    effects.clearSorts();
    await running;
  });

  it('renders a sort without caster origin and resolves every running promise when cleared', async () => {
    mocks.shapeCells.mockReturnValue([{ x: 0, y: 0, d: 0 }]);
    const effects = useSortEffects();
    const first = effects.launchSort('known', 0, 0, { width: 1, height: 1, elevation: [0] }, legacyProjection);
    const second = effects.launchSort('known', 0, 0, { width: 1, height: 1, elevation: [0] }, legacyProjection);
    const ctx = {} as CanvasRenderingContext2D;

    effects.renderSorts(ctx);
    expect(mocks.playSort).toHaveBeenCalledWith(
      ctx,
      'known',
      expect.any(Array),
      expect.any(Number),
      null,
      '#8b9dcf',
    );

    effects.clearSorts();
    await Promise.all([first, second]);
    expect(effects.activeSorts.value).toHaveLength(0);
  });
});
