export type InterludeNodeDto = {
  id: string;
  type: string;
  label: string;
  description: string;
  positionSlot: string;
  isEnabled: boolean;
  actionKey: string;
};

export type InterludeActionDto = {
  key: string;
  label: string;
  description: string;
  requiresConfirmation: boolean;
  isDangerous: boolean;
  isEnabled: boolean;
};

export type RunSummaryDto = {
  seed: string;
  currentRoomIndex: number;
  displayRoomNumber: number;
  currentRoomType: string;
  activePalaceLawCount: number;
};

export type InterludeDto = {
  runId: string;
  currentRoomIndex: number;
  displayRoomNumber: number;
  nodes: InterludeNodeDto[];
  availableActions: InterludeActionDto[];
  runSummary: RunSummaryDto;
};

export type EnterInterludeApiResponse = {
  interlude: InterludeDto;
};

export type GetInterludeApiResponse = {
  interlude: InterludeDto;
};

export function unwrapInterludeResponse(
  response: EnterInterludeApiResponse | GetInterludeApiResponse,
): InterludeDto {
  return response.interlude;
}

/** Maps a positionSlot string to a CSS grid area name. */
export function positionSlotToGridArea(slot: string): string {
  const map: Record<string, string> = {
    'center':       'center',
    'top':          'top',
    'left':         'left',
    'right':        'right',
    'bottom-left':  'bl',
    'bottom-right': 'br',
  };
  return map[slot] ?? 'center';
}

/** Returns the Unicode glyph for an InterludeNodeType string. */
export function nodeTypeGlyph(type: string): string {
  const glyphs: Record<string, string> = {
    Player:      '◉',
    Elise:       '✦',
    Inventory:   '◈',
    Journal:     '✧',
    Placeholder: '○',
  };
  return glyphs[type] ?? '○';
}
