import type { CombatRuntimeDto } from '../../combat/types/combatContracts';
import type { RunDto } from '../../runs/types/runTypes';
import type { PlayerProfileView } from '../../party/types/playerTypes';

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

export type DevToolsPsycheStep = {
  depth: number;
  dominant: string;
  distribution: Record<string, number>;
};

export type DevToolsRunPsycheResponse = {
  runId: string;
  dominant: string;
  current: Record<string, number>;
  trajectory: DevToolsPsycheStep[];
};

export type PalaceRoomStateKey = 'Neutral' | 'Silent' | 'Painful' | 'Enraged' | 'Violent';

export type RoomClimateKey = 'None' | 'Grey' | 'Rain' | 'Heatwave' | 'Hail';

export type DevToolsStatusKey = 'unknown' | 'available' | 'unavailable';

export type DevToolsPlayerDebugResponse = {
  message: string;
  profile: PlayerProfileView;
};
