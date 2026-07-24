import { describe, expect, it } from 'vitest';
import { computed } from 'vue';
import {
  useRoomBackdropTheme,
  THEME_BACKDROP_CLASS,
  THEME_ACCENT,
  THEME_PALETTE_3D,
} from './useRoomBackdropTheme';
import type { RoomDto } from '../../runs/types/runTypes';

function makeRoom(id: string, theme: string): RoomDto {
  return {
    id,
    depth: 0,
    roomType: 'Combat',
    theme,
    state: 'Active',
    currentNodeDepth: 0,
    maxNodeDepth: 0,
    totalNodeCount: 0,
    bossPreview: { bossId: 'boss-1', name: 'Boss', roomType: 'RoomBoss', dangerHint: 'High' },
    nodes: [],
    availableNodes: [],
    layoutTemplateKey: null,
    layoutTemplateVersion: null,
    grid: {
      width: 1, height: 1, movementBudget: 1, movementBudgetRemaining: 1,
      partyX: 0, partyY: 0, canChallengeBossRemotely: false, revealedCells: [],
    },
  };
}

describe('useRoomBackdropTheme', () => {
  it('is deterministic: the same roomId always yields the same nuance', () => {
    const roomA = computed(() => makeRoom('room-fixed', 'Threshold'));
    const roomB = computed(() => makeRoom('room-fixed', 'Threshold'));

    expect(useRoomBackdropTheme(roomA).roomNuance.value)
      .toBe(useRoomBackdropTheme(roomB).roomNuance.value);
  });

  it('falls back to the default palette for an unknown theme', () => {
    const room = computed(() => makeRoom('room-1', 'SomeFutureTheme'));
    const { palette3D } = useRoomBackdropTheme(room);
    expect(palette3D.value).toBeDefined();
  });

  it('every CSS backdrop theme has a corresponding 3D palette entry — nothing silently falls back', () => {
    for (const theme of Object.keys(THEME_BACKDROP_CLASS)) {
      expect(THEME_PALETTE_3D[theme], `missing 3D palette for theme "${theme}"`).toBeDefined();
    }
  });

  it('every CSS backdrop theme also has a 2D accent entry', () => {
    for (const theme of Object.keys(THEME_BACKDROP_CLASS)) {
      expect(THEME_ACCENT[theme], `missing accent for theme "${theme}"`).toBeDefined();
    }
  });

  it('only the Final theme carries a pulse speed', () => {
    for (const [theme, palette] of Object.entries(THEME_PALETTE_3D)) {
      if (theme === 'Final') {
        expect(palette.pulseSpeed).toBeGreaterThan(0);
      } else {
        expect(palette.pulseSpeed).toBeUndefined();
      }
    }
  });
});
