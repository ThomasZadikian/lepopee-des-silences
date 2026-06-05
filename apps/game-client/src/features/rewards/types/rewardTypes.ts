import type { RunDto } from '../../runs/types/runTypes';

export type RewardOptionDto = {
  id?: string;
  rewardId?: string;
  key?: string;
  name?: string;
  displayName?: string;
  rarity?: string;
  rewardType?: string;
  description?: string;
  isSelected?: boolean;
};

export type RewardOfferDto = {
  id: string;
  runId?: string;
  source?: string;
  title?: string;
  description?: string;
  options: RewardOptionDto[];
};

export type PendingRewardOfferResponse =
  | RewardOfferDto
  | { rewardOffer: RewardOfferDto }
  | { offer: RewardOfferDto };

export type SelectRewardRequest = {
  rewardOfferId?: string;
  rewardOptionId?: string;
  optionId?: string;
};

export type SelectRewardResponse =
  | RunDto
  | { run: RunDto }
  | { rewardOffer: RewardOfferDto }
  | unknown;

export function unwrapRewardOffer(
  response: PendingRewardOfferResponse,
): RewardOfferDto {
  if ('rewardOffer' in response) {
    return response.rewardOffer;
  }

  if ('offer' in response) {
    return response.offer;
  }

  return response;
}