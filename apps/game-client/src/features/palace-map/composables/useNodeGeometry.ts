import * as THREE from 'three';

export type NodeGeometrySpec = {
  geometryFactory: () => THREE.BufferGeometry;
  materialParams: THREE.MeshStandardMaterialParameters;
  /** Uniform scale applied to the whole marker group. */
  scale: number;
};

// Approximate hex equivalents of the CSS oklch tokens in tokens.css (--gold/--frost/
// --blood/--sap) — Three.js materials need a concrete color, not a CSS custom
// property. Purely a starting palette; not meant to be a pixel-exact conversion.
const TONE_HEX = {
  gold: 0xcbb26a,
  frost: 0xa7a8e8,
  blood: 0xe08a8a,
  sap: 0x8fd9b0,
} as const;

/**
 * One procedural-primitive analogue per SigilIcon kind (see SigilIcon.vue) — the 3D
 * marker equivalent of each node's flat 2D glyph. Each entry mirrors the glyph's own
 * silhouette intent (cone = "danger point", torus = "ring", octahedron = "faceted
 * gem/diamond") rather than being a literal extrusion of the SVG path.
 */
export const NODE_GEOMETRY_BY_SIGIL_KIND: Record<string, NodeGeometrySpec> = {
  // combat: triangle → a single upward cone, danger reads as a spike.
  combat: {
    geometryFactory: () => new THREE.ConeGeometry(0.5, 1, 4),
    materialParams: { color: TONE_HEX.blood, roughness: 0.55, metalness: 0.1 },
    scale: 1,
  },
  // elite: triangle + circle → the same cone, ringed by a torus (the glyph's circle).
  elite: {
    geometryFactory: () => mergeGeometries([
      translateGeometry(new THREE.ConeGeometry(0.42, 0.9, 4), 0, 0.15, 0),
      rotateGeometry(new THREE.TorusGeometry(0.55, 0.06, 8, 24), Math.PI / 2, 0, 0),
    ]),
    materialParams: { color: TONE_HEX.blood, roughness: 0.5, metalness: 0.15 },
    scale: 1.1,
  },
  // memoire: double circle → two concentric rings on the ground plane.
  memoire: {
    geometryFactory: () => mergeGeometries([
      rotateGeometry(new THREE.TorusGeometry(0.5, 0.05, 8, 24), Math.PI / 2, 0, 0),
      rotateGeometry(new THREE.TorusGeometry(0.22, 0.05, 8, 20), Math.PI / 2, 0, 0),
    ]),
    materialParams: { color: TONE_HEX.frost, roughness: 0.4, metalness: 0.2 },
    scale: 1,
  },
  // repos: crescent → a torus with a partial arc (an open ring, not a full circle).
  repos: {
    geometryFactory: () => new THREE.TorusGeometry(0.5, 0.16, 10, 24, Math.PI * 1.5),
    materialParams: { color: TONE_HEX.sap, roughness: 0.6, metalness: 0.05 },
    scale: 1,
  },
  // marchand: hollow diamond → a wireframe octahedron.
  marchand: {
    geometryFactory: () => new THREE.OctahedronGeometry(0.55, 0),
    materialParams: { color: TONE_HEX.gold, roughness: 0.5, metalness: 0.1, wireframe: true },
    scale: 1,
  },
  // loi: filled diamond → a solid octahedron.
  loi: {
    geometryFactory: () => new THREE.OctahedronGeometry(0.5, 0),
    materialParams: { color: TONE_HEX.gold, roughness: 0.4, metalness: 0.2 },
    scale: 1,
  },
  // malediction: dashed circle → a torus built from arc segments (gaps between them).
  malediction: {
    geometryFactory: () => mergeGeometries(
      Array.from({ length: 5 }, (_, i) => {
        const arc = new THREE.TorusGeometry(0.5, 0.07, 6, 8, (Math.PI * 2) / 5 * 0.6);
        return rotateGeometry(arc, Math.PI / 2, 0, (Math.PI * 2 * i) / 5);
      }),
    ),
    materialParams: { color: TONE_HEX.blood, roughness: 0.7, metalness: 0.05 },
    scale: 1,
  },
  // pnj: circle + dot → a sphere with a smaller inner sphere.
  pnj: {
    geometryFactory: () => mergeGeometries([
      new THREE.SphereGeometry(0.45, 16, 12),
      new THREE.SphereGeometry(0.12, 10, 8),
    ]),
    materialParams: { color: TONE_HEX.frost, roughness: 0.45, metalness: 0.1 },
    scale: 1,
  },
  // objet: compact filled diamond → a small octahedron.
  objet: {
    geometryFactory: () => new THREE.OctahedronGeometry(0.38, 0),
    materialParams: { color: TONE_HEX.gold, roughness: 0.35, metalness: 0.25 },
    scale: 0.9,
  },
  // rare: 4-point star → an octahedron stretched along Y as a cheaper stand-in for a
  // fully extruded star shape (avoids authoring/loading a THREE.Shape at runtime).
  rare: {
    geometryFactory: () => scaleGeometry(new THREE.OctahedronGeometry(0.45, 0), 1, 1.5, 1),
    materialParams: { color: TONE_HEX.blood, roughness: 0.3, metalness: 0.3, emissive: TONE_HEX.blood, emissiveIntensity: 0.25 },
    scale: 1,
  },
  // boss: triple circle + diamond → nested tori around a solid, emissive octahedron core.
  boss: {
    geometryFactory: () => mergeGeometries([
      new THREE.OctahedronGeometry(0.4, 0),
      rotateGeometry(new THREE.TorusGeometry(0.65, 0.05, 8, 24), Math.PI / 2, 0, 0),
      rotateGeometry(new THREE.TorusGeometry(0.85, 0.04, 8, 24), Math.PI / 2, 0, 0),
    ]),
    materialParams: { color: TONE_HEX.blood, roughness: 0.35, metalness: 0.25, emissive: TONE_HEX.blood, emissiveIntensity: 0.4 },
    scale: 1.4,
  },
};

const FALLBACK_GEOMETRY_SPEC: NodeGeometrySpec = {
  geometryFactory: () => new THREE.OctahedronGeometry(0.4, 0),
  materialParams: { color: 0x9a9a9a, roughness: 0.5, metalness: 0.1 },
  scale: 0.9,
};

export function useNodeGeometry() {
  function geometrySpecFor(sigilKind: string): NodeGeometrySpec {
    return NODE_GEOMETRY_BY_SIGIL_KIND[sigilKind] ?? FALLBACK_GEOMETRY_SPEC;
  }

  return { geometrySpecFor };
}

// ── Small local geometry helpers (avoid pulling in three-stdlib's BufferGeometryUtils
// for just a handful of translate/rotate/scale/merge calls). ───────────────────────
function translateGeometry(geometry: THREE.BufferGeometry, x: number, y: number, z: number) {
  geometry.translate(x, y, z);
  return geometry;
}

function rotateGeometry(geometry: THREE.BufferGeometry, x: number, y: number, z: number) {
  geometry.rotateX(x);
  geometry.rotateY(y);
  geometry.rotateZ(z);
  return geometry;
}

function scaleGeometry(geometry: THREE.BufferGeometry, x: number, y: number, z: number) {
  geometry.scale(x, y, z);
  return geometry;
}

/**
 * Merges multiple geometries into one by concatenating their position/normal
 * attributes and index buffers, offsetting indices per part. Deliberately minimal
 * (no UV/material-group handling) since these marker shapes use a single flat color.
 */
function mergeGeometries(geometries: THREE.BufferGeometry[]): THREE.BufferGeometry {
  const merged = new THREE.BufferGeometry();
  const positions: number[] = [];
  const normals: number[] = [];
  const indices: number[] = [];
  let vertexOffset = 0;

  for (const geometry of geometries) {
    const positionAttr = geometry.getAttribute('position');
    const normalAttr = geometry.getAttribute('normal');
    for (let i = 0; i < positionAttr.count; i++) {
      positions.push(positionAttr.getX(i), positionAttr.getY(i), positionAttr.getZ(i));
      if (normalAttr) {
        normals.push(normalAttr.getX(i), normalAttr.getY(i), normalAttr.getZ(i));
      }
    }
    const index = geometry.getIndex();
    if (index) {
      for (let i = 0; i < index.count; i++) {
        indices.push(index.getX(i) + vertexOffset);
      }
    }
    vertexOffset += positionAttr.count;
  }

  merged.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
  if (normals.length === positions.length) {
    merged.setAttribute('normal', new THREE.Float32BufferAttribute(normals, 3));
  } else {
    merged.computeVertexNormals();
  }
  if (indices.length > 0) {
    merged.setIndex(indices);
  }
  return merged;
}
