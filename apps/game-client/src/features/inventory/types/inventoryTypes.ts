import type { CombatLogEntryDto, CombatRuntimeDto } from '../../combat/types/combatContracts';

export interface RunItemDto {
  id: string;
  definitionKey: string;
  displayName: string;
  description: string;
  type: 'Consumable' | 'Passive' | 'Fragment' | 'Equipment' | 'Relic' | 'Grimoire' | 'WeatherInstrument' | 'SkillEssence' | 'Weapon';
  rarity: 'Common' | 'Uncommon' | 'Rare' | 'Epic';
  quantity: number;
  effectType: string;
  effectAmount: number;
  isUsable: boolean;
}

export interface UseGrimoireResponse {
  runId: string;
  itemId: string;
  characterId: string;
  grantedSkillKey?: string | null;
  teamSkillPointsGranted: number;
  itemDepleted: boolean;
}

export interface GetRunInventoryResponse {
  runId: string;
  items: RunItemDto[];
}

export interface UseRunItemResponse {
  runId: string;
  itemId: string;
  effectType: string;
  effectAmount: number;
  itemDepleted: boolean;
  usedInCombat: boolean;
  playerState: {
    currentVitality: number;
    maxVitality: number;
    guard: number;
    mana: number;
    charge: number;
  };
  combat?: CombatRuntimeDto | null;
  logEntries?: CombatLogEntryDto[] | null;
}
