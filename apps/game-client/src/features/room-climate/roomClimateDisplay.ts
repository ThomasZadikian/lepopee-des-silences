import type { RoomClimateStateDto } from '../runs/types/runTypes';

export type RoomClimateDisplay = {
  key: string | null;
  type: string | null;
  displayName: string;
  description: string | null;
  source: string | null;
  duration: string;
  roomId: string | null;
};

const TYPE_LABELS: Record<string, { displayName: string; description: string }> = {
  Grey: {
    displayName: 'Grisaille',
    description: 'Une grisaille pèse sur les actions dans cette Room.',
  },
  Rain: {
    displayName: 'Pluie',
    description: 'La pluie accélère le rythme de cette Room.',
  },
  Heatwave: {
    displayName: 'Canicule',
    description: 'La chaleur altère les soins reçus dans cette Room.',
  },
  Hail: {
    displayName: 'Grêle',
    description: 'La grêle rend les effets persistants plus menaçants dans cette Room.',
  },
};

export function resolveRoomClimateDisplay(
  climate: RoomClimateStateDto | null | undefined,
): RoomClimateDisplay | null {
  if (!climate) return null;

  if (typeof climate === 'string') {
    const fallback = TYPE_LABELS[climate];
    return {
      key: climate,
      type: climate,
      displayName: fallback?.displayName ?? climate,
      description: fallback?.description ?? null,
      source: null,
      duration: 'Room actuelle uniquement',
      roomId: null,
    };
  }

  const type = climate.type ?? climate.key ?? null;
  const fallback = type ? TYPE_LABELS[type] : undefined;

  return {
    key: climate.key ?? null,
    type,
    displayName: climate.displayName ?? fallback?.displayName ?? type ?? 'Climat actif',
    description: climate.description ?? fallback?.description ?? null,
    source: climate.source ?? null,
    duration: climate.expiresWhen ?? climate.expiresAt ?? 'Room actuelle uniquement',
    roomId: climate.roomId ?? null,
  };
}
