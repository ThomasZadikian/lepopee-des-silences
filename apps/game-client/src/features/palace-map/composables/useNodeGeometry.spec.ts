import { describe, expect, it } from 'vitest';
import { useNodeGeometry, NODE_GEOMETRY_BY_SIGIL_KIND } from './useNodeGeometry';
import { SIGIL_KIND_BY_NODE_TYPE } from './useNodePresentation';

describe('useNodeGeometry', () => {
  const { geometrySpecFor } = useNodeGeometry();

  it('every sigil kind produced by SIGIL_KIND_BY_NODE_TYPE has its own 3D geometry entry — nothing silently falls back', () => {
    const kinds = new Set(Object.values(SIGIL_KIND_BY_NODE_TYPE));
    for (const kind of kinds) {
      expect(NODE_GEOMETRY_BY_SIGIL_KIND[kind], `missing 3D geometry for sigil kind "${kind}"`).toBeDefined();
    }
  });

  it('produces a real, non-degenerate geometry for every known kind', () => {
    for (const kind of Object.keys(NODE_GEOMETRY_BY_SIGIL_KIND)) {
      const spec = geometrySpecFor(kind);
      const geometry = spec.geometryFactory();
      const position = geometry.getAttribute('position');
      expect(position, `kind "${kind}" produced a geometry with no position attribute`).toBeDefined();
      expect(position.count, `kind "${kind}" produced an empty geometry`).toBeGreaterThan(0);
      expect(spec.scale).toBeGreaterThan(0);
    }
  });

  it('falls back to a generic geometry for an unknown kind instead of throwing', () => {
    expect(() => geometrySpecFor('some-future-kind')).not.toThrow();
    const spec = geometrySpecFor('some-future-kind');
    expect(spec.geometryFactory().getAttribute('position').count).toBeGreaterThan(0);
  });

  it('gives the boss marker an emissive material and a larger scale than a plain combat node', () => {
    const boss = geometrySpecFor('boss');
    const combat = geometrySpecFor('combat');
    expect(boss.materialParams.emissive).toBeDefined();
    expect(boss.scale).toBeGreaterThan(combat.scale);
  });
});
