import { describe, expect, it } from 'vitest';
import { buildBattlePlan, type BuildBattlePlanInput } from './useTacticalBattlePlan';

// Minimal 2x1 battlefield: two walkable floor cells, no obstacles.
function baseInput(overrides: Partial<BuildBattlePlanInput> = {}): BuildBattlePlanInput {
  return {
    canvasWidth: 400,
    canvasHeight: 300,
    gridWidth: 2,
    gridHeight: 1,
    elevation: [0, 0],
    walkable: [true, true],
    theme: 'Threshold',
    ambientTint: 'neutral',
    ...overrides,
  };
}

describe('buildBattlePlan — node decor', () => {
  it('paints an unresolved node cell as a prop, on top of its floor tile', () => {
    const plan = buildBattlePlan(baseInput({
      nodesByCell: new Map([['1,0', { type: 'Merchant', state: 'Available' }]]),
    }));

    const propEntry = plan.find((e) => e.spriteKey.kind === 'prop');
    expect(propEntry).toBeDefined();
    expect(propEntry?.x).toBe(1);
    expect(propEntry?.y).toBe(0);

    const floorEntry = plan.find((e) => e.cellKey === '1,0');
    expect(floorEntry?.spriteKey.kind).toBe('floor');
    // The prop's sortKey must land strictly after its own floor tile's, so it paints on top.
    expect(propEntry!.sortKey).toBeGreaterThan(floorEntry!.sortKey);
  });

  it('does not paint a resolved node as a prop', () => {
    const plan = buildBattlePlan(baseInput({
      nodesByCell: new Map([['0,0', { type: 'Merchant', state: 'Resolved' }]]),
    }));

    expect(plan.some((e) => e.spriteKey.kind === 'prop')).toBe(false);
  });

  it('paints no prop for a node type with no authored decor and no contact behavior', () => {
    const plan = buildBattlePlan(baseInput({
      nodesByCell: new Map([['0,0', { type: 'SomeUnknownType', state: 'Available' }]]),
    }));

    expect(plan.some((e) => e.spriteKey.kind === 'prop')).toBe(false);
  });

  it('does nothing when nodesByCell is omitted (unaffected battles keep their old output)', () => {
    const plan = buildBattlePlan(baseInput());
    expect(plan.some((e) => e.spriteKey.kind === 'prop')).toBe(false);
  });
});
