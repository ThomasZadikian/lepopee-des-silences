import type { RunDto } from '../../runs/types/runTypes';

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
  runId?: string;
  source?: string;
  state?: string;
  status?: string;
  title?: string;
  description?: string;
  choices?: RewardChoiceDto[];
  options?: RewardOptionDto[];
  selectedChoiceId?: string | null;
  selectedOptionId?: string | null;
};

export type PendingRewardOfferResponse =
  | RewardOfferDto
  | { rewardOffer: RewardOfferDto }
  | { offer: RewardOfferDto };

export type SelectRewardRequest = {
  rewardOfferId?: string;
  rewardChoiceId?: string;
  rewardOptionId?: string;
  choiceId?: string;
  optionId?: string;
};

export type SelectRewardResponse =
  | RunDto
  | { run: RunDto }
  | { rewardOffer: RewardOfferDto }
  | { offer: RewardOfferDto }
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