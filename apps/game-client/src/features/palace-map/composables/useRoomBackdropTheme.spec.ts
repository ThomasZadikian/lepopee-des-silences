import { describe, expect, it } from 'vitest';
import { computed } from 'vue';
import type { RoomDto } from '../../runs/types/runTypes';
import {
  PAINTED_THEMES,
  THEME_ACCENT,
  resolvePaintedTheme,
  useRoomBackdropTheme,
} from './useRoomBackdropTheme';

function makeRoom(theme: string): RoomDto {
  return { id: 'room-1', theme } as RoomDto;
}

describe('resolvePaintedTheme', () => {
  it('passes every backend theme through untouched', () => {
    // These 7 strings are what RoomThemeResolver.cs emits and what the tile engine paints;
    // if the backend grows an 8th, this is the test that should force a decision here.
    for (const theme of PAINTED_THEMES) {
      expect(resolvePaintedTheme(theme)).toBe(theme);
    }
  });

  it('falls back to Threshold rather than leaving a room unpainted', () => {
    for (const unknown of ['La Forêt', '', 'threshold', null, undefined]) {
      expect(resolvePaintedTheme(unknown)).toBe('Threshold');
    }
  });
});

describe('useRoomBackdropTheme', () => {
  it('exposes the painted theme for the tile engine', () => {
    const { paintedTheme } = useRoomBackdropTheme(computed(() => makeRoom('Forest')));
    expect(paintedTheme.value).toBe('Forest');
  });

  it('exposes the accent token the surrounding DOM chrome borrows', () => {
    const { themeAccent } = useRoomBackdropTheme(computed(() => makeRoom('Antechamber')));
    expect(themeAccent.value).toBe('--gold');
  });

  it('falls back to a usable accent for an unknown theme', () => {
    const { themeAccent } = useRoomBackdropTheme(computed(() => makeRoom('La Forêt')));
    expect(themeAccent.value).toBe('--gold');
  });

  it('gives every painted theme an accent token', () => {
    for (const theme of PAINTED_THEMES) {
      expect(THEME_ACCENT[theme]).toBeTruthy();
    }
  });
});
