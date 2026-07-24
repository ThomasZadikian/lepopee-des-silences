import { computed, type ComputedRef } from 'vue';
import type { RoomDto } from '../../runs/types/runTypes';
import { hashSeed } from './usePalaceTerrain';

// ── Room backdrop: thematic, coherent within a theme, lightly nuanced per room ─────
export const THEME_BACKDROP_CLASS: Record<string, string> = {
  Threshold: 'tgrid__backdrop--threshold',
  Memory: 'tgrid__backdrop--memory',
  Forest: 'tgrid__backdrop--forest',
  Rupture: 'tgrid__backdrop--rupture',
  Silence: 'tgrid__backdrop--silence',
  Antechamber: 'tgrid__backdrop--antechamber',
  Final: 'tgrid__backdrop--final',
};

// Tiles borrow the room's accent color too — an "Antechamber" floor reads gold, a
// "Forest" floor reads green, etc. — instead of every room sharing the same neutral
// grey tile regardless of theme.
export const THEME_ACCENT: Record<string, string> = {
  Threshold: '--frost',
  Memory: '--gold',
  Forest: '--sap',
  Rupture: '--blood',
  Silence: '--frost',
  Antechamber: '--gold',
  Final: '--blood',
};

export function useRoomBackdropTheme(room: ComputedRef<RoomDto>) {
  const backdropClass = computed(() =>
    THEME_BACKDROP_CLASS[room.value.theme] ?? 'tgrid__backdrop--default');

  // Same room + same theme always renders the same backdrop family; this is the only
  // thing that varies it slightly between two rooms sharing a theme (a small hue drift).
  const roomNuance = computed(() => hashSeed(room.value.id ?? '') % 100);

  const themeAccent = computed(() => THEME_ACCENT[room.value.theme] ?? '--gold');

  return { backdropClass, roomNuance, themeAccent };
}
