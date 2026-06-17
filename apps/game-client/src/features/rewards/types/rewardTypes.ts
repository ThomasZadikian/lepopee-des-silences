import type { RunDto } from '../../runs/types/runTypes';

export type CombatScalingDto = {
  combatTier: string;
  baseRisk: number;
  actualRisk: number;
  riskDelta: number;
  difficultyMultiplier: number;
  rewardPowerMultiplier: number;
  riskBand: string;
};

export type RewardChoiceDto = {
  id: string;
  rewardType: string;
  label: string;
  description: string;
  payloadKey?: string;
  rarity?: string;
  isSelected?: boolean;
};

export type RewardOptionDto = {
  id?: string;
  rewardId?: string;
  key?: string;
  name?: string;
  displayName?: string;
  label?: string;
  rarity?: string;
  rewardType?: string;
  type?: string;
  description?: string;
  payloadKey?: string;
  isSelected?: boolean;
};

export type RewardOfferDto = {
  id: string;
  source?: string;
  state?: string;
  choices?: RewardChoiceDto[];
  selectedChoiceId?: string | null;
  combatScaling?: CombatScalingDto | null;
  runId?: string;
  status?: string;
  title?: string;
  description?: string;
  options?: RewardOptionDto[];
  selectedOptionId?: string | null;
};

export type PendingRewardOfferResponse =
  | RewardOfferDto
  | { rewardOffer: RewardOfferDto }
  | { offer: RewardOfferDto };

export type SelectRewardRequest = {
  choiceId: string;
};

export type SelectRewardResponse =
  | { run: RunDto; rewardOffer: RewardOfferDto }
  | { run: RunDto }
  | RunDto
  | unknown;

export function unwrapRewardOffer(response: PendingRewardOfferResponse): RewardOfferDto {
  if (typeof response === 'object' && response !== null && 'rewardOffer' in response) {
    return (response as { rewardOffer: RewardOfferDto }).rewardOffer;
  }
  if (typeof response === 'object' && response !== null && 'offer' in response) {
    return (response as { offer: RewardOfferDto }).offer;
  }
  return response as RewardOfferDto;
}

export function unwrapRunFromSelectRewardResponse(response: SelectRewardResponse): RunDto | null {
  if (!response || typeof response !== 'object') return null;
  const r = response as Record<string, unknown>;
  if ('run' in r && typeof r['run'] === 'object' && r['run'] !== null && 'id' in (r['run'] as object)) {
    return r['run'] as RunDto;
  }
  if ('id' in r && 'currentRoom' in r) {
    return response as RunDto;
  }
  return null;
}
