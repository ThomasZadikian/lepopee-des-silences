import type { CombatRuntimeDto } from '../../combat/types/combatContracts';
import type { RunDto } from '../../runs/types/runTypes';

export type DevToolsStatusResponse = {
  enabled: boolean;
  environment: string;
};

export type DevToolsRunResponse = {
  message: string;
  run: RunDto;
};

export type DevToolsCombatResponse = {
  message: string;
  combat: CombatRuntimeDto;
};

export type PalaceRoomStateKey = 'Neutral' | 'Silent' | 'Painful' | 'Enraged' | 'Violent';

export type RoomClimateKey = 'None' | 'Grey' | 'Rain' | 'Heatwave' | 'Hail';

export type DevToolsStatusKey = 'unknown' | 'available' | 'unavailable';
