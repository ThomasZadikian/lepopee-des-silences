import { computed, type ComputedRef } from 'vue';
import type { RoomDto } from '../../runs/types/runTypes';
import { resolveRoomVisual, type RenderTheme } from './useTerrainSprites';

/**
 * Resolves a room's theme into the two things the renderer needs from it: the painted-tile
 * theme (materials, palette, backdrop scenery) and the accent token the UI chrome borrows.
 *
 * The room backdrop used to be seven stacks of CSS gradients declared in TacticalGridMap.vue,
 * nuanced per room by a hue-rotate. It is now painted into the canvas by the tile engine
 * (`drawBackdrop`), which takes the room id as its own seed — so the CSS class table and the
 * hue-drift helper that fed it are gone rather than kept unused.
 */

/** The 7 generic render themes. Exactly the strings the backend's RoomThemeResolver emits
 * (`RoomThemeResolver.cs`) — the two lists must stay in step. These are the FALLBACK: a room
 * the catalogue names gets its own palette instead (see paintedTheme below). */
export const PAINTED_THEMES: readonly RenderTheme[] = [
  'Threshold', 'Memory', 'Forest', 'Rupture', 'Silence', 'Antechamber', 'Final',
];

const PAINTED_THEME_SET = new Set<string>(PAINTED_THEMES);

/** Falls back to Threshold rather than painting an unthemed void: an unknown theme means the
 * backend grew a room type the renderer hasn't been taught yet, which should still be playable. */
export function resolvePaintedTheme(theme: string | null | undefined): RenderTheme {
  return PAINTED_THEME_SET.has(theme ?? '') ? (theme as RenderTheme) : 'Threshold';
}

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
  /**
   * What the renderer actually paints with. The 27 canon Palace rooms each carry their own
   * palette in the tile engine, keyed by the catalogue key the DTO already sends; anything the
   * catalogue does not name — a room type with no canon entry — falls back to its generic
   * render theme. Without this, every room in the Palace collapsed onto 7 shared palettes and
   * the entrance hall could come out with the same floor as the fourth circle of hell.
   */
  const paintedTheme = computed(() => resolveRoomVisual(
    room.value.catalogRoomKey ?? undefined,
    resolvePaintedTheme(room.value.theme),
  ));

  const themeAccent = computed(() => THEME_ACCENT[room.value.theme] ?? '--gold');

  return { paintedTheme, themeAccent };
}
