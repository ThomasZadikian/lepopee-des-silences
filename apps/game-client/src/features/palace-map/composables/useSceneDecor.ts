import * as THREE from 'three';
import { computed, type ComputedRef } from 'vue';
import type { RoomDto, RoomGridDto } from '../../runs/types/runTypes';
import { hashSeed, mulberry32 } from './usePalaceTerrain';
import { TILE_SIZE } from '../scene/sceneConstants';

export type DecorPart = {
  geometryFactory: () => THREE.BufferGeometry;
  /** Local offset from the prop's own position (Y is "up" from the ground). */
  offset: [number, number, number];
  color: number;
  emissive: number;
  emissiveIntensity: number;
  roughness: number;
  metalness: number;
};

export type DecorPropSpec = {
  position: [number, number, number];
  rotationY: number;
  scale: number;
  parts: DecorPart[];
};

type ThemeDecorKind = 'pillar' | 'monolith' | 'tree' | 'crystal' | 'spike';

const THEME_DECOR_KIND: Record<string, ThemeDecorKind> = {
  Threshold: 'pillar',
  Memory: 'monolith',
  Forest: 'tree',
  Rupture: 'crystal',
  Silence: 'monolith',
  Antechamber: 'pillar',
  Final: 'spike',
};

const DEFAULT_DECOR_KIND: ThemeDecorKind = 'pillar';

/** How many props ring each room — a handful is enough to read as "a place", not a forest. */
const PROP_COUNT = 7;
/** How far outside the grid's own footprint props are scattered, in tile units. */
const RING_MIN_MARGIN = 1.2;
const RING_MAX_MARGIN = 3.4;

function buildParts(kind: ThemeDecorKind, rng: () => number, accentColor: number): DecorPart[] {
  const base = { emissive: accentColor, roughness: 0.6, metalness: 0.15 };

  switch (kind) {
    case 'pillar': {
      // Threshold/Antechamber — a slender formal column, softly glowing at the top.
      const height = 2.4 + rng() * 1.4;
      return [
        {
          geometryFactory: () => new THREE.CylinderGeometry(0.16, 0.22, height, 8),
          offset: [0, height / 2, 0],
          color: accentColor, emissiveIntensity: 0, ...base,
        },
        {
          geometryFactory: () => new THREE.OctahedronGeometry(0.22, 0),
          offset: [0, height + 0.15, 0],
          color: accentColor, emissiveIntensity: 0.5, ...base,
        },
      ];
    }
    case 'monolith': {
      // Memory/Silence — a still, weathered slab, barely lit from within.
      const height = 1.7 + rng() * 1.1;
      return [{
        geometryFactory: () => new THREE.BoxGeometry(0.34, height, 0.34),
        offset: [0, height / 2, 0],
        color: accentColor, emissiveIntensity: 0.08, ...base,
      }];
    }
    case 'tree': {
      // Forest — a trunk plus a stacked canopy, echoing the interior node cones.
      const trunkHeight = 0.7 + rng() * 0.3;
      const canopyHeight = 1.1 + rng() * 0.8;
      return [
        {
          geometryFactory: () => new THREE.CylinderGeometry(0.09, 0.13, trunkHeight, 6),
          offset: [0, trunkHeight / 2, 0],
          color: 0x3a2a1c, emissiveIntensity: 0, emissive: 0x000000, roughness: 0.85, metalness: 0,
        },
        {
          geometryFactory: () => new THREE.ConeGeometry(0.55, canopyHeight, 7),
          offset: [0, trunkHeight + canopyHeight / 2.4, 0],
          color: accentColor, emissiveIntensity: 0.05, ...base,
        },
      ];
    }
    case 'crystal': {
      // Rupture — a jagged cluster of shards at slightly different heights/angles.
      return Array.from({ length: 3 }, (_, i) => {
        const height = 0.6 + rng() * 1.2;
        return {
          geometryFactory: () => new THREE.ConeGeometry(0.16 + rng() * 0.1, height, 5),
          offset: [(rng() - 0.5) * 0.4, height / 2, (rng() - 0.5) * 0.4] as [number, number, number],
          color: accentColor, emissiveIntensity: 0.35, ...base, metalness: 0.3,
        };
      });
    }
    case 'spike': {
      // Final — broken, oppressive spikes, more emissive than any other theme's decor.
      return Array.from({ length: 2 }, (_, i) => {
        const height = 1 + rng() * 1.6;
        return {
          geometryFactory: () => new THREE.ConeGeometry(0.2, height, 4),
          offset: [(rng() - 0.5) * 0.5, height / 2, (rng() - 0.5) * 0.5] as [number, number, number],
          color: accentColor, emissiveIntensity: 0.5, ...base, roughness: 0.4,
        };
      });
    }
  }
}

export function useSceneDecor(
  room: ComputedRef<RoomDto>,
  grid: ComputedRef<RoomGridDto | null>,
  accentColor: ComputedRef<number>,
) {
  const decorProps = computed<DecorPropSpec[]>(() => {
    const g = grid.value;
    if (!g) return [];

    const kind = THEME_DECOR_KIND[room.value.theme] ?? DEFAULT_DECOR_KIND;
    const rng = mulberry32(hashSeed(`${room.value.id}:decor`));
    const centerX = ((g.width - 1) / 2) * TILE_SIZE;
    const centerZ = ((g.height - 1) / 2) * TILE_SIZE;
    // Half-diagonal of the grid's own footprint — props scatter in a ring starting
    // just past this radius so they never overlap the actual playable tiles.
    const gridRadius = Math.sqrt(g.width ** 2 + g.height ** 2) / 2;

    return Array.from({ length: PROP_COUNT }, () => {
      const angle = rng() * Math.PI * 2;
      const radius = gridRadius + RING_MIN_MARGIN + rng() * (RING_MAX_MARGIN - RING_MIN_MARGIN);
      const position: [number, number, number] = [
        centerX + Math.cos(angle) * radius,
        0,
        centerZ + Math.sin(angle) * radius,
      ];
      return {
        position,
        rotationY: rng() * Math.PI * 2,
        scale: 0.85 + rng() * 0.4,
        parts: buildParts(kind, rng, accentColor.value),
      };
    });
  });

  return { decorProps };
}
