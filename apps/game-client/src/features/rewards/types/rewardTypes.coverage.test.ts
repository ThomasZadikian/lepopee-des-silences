import { describe, expect, it } from 'vitest';
import { unwrapRewardOffer, unwrapRunFromSelectRewardResponse } from './rewardTypes';

const offer = { id: 'offer-1', choices: [] } as any;
const run = { id: 'run-1', currentRoom: {} } as any;

describe('reward response normalizers coverage margin', () => {
  it('unwraps every pending-offer response shape', () => {
    expect(unwrapRewardOffer({ rewardOffer: offer })).toBe(offer);
    expect(unwrapRewardOffer({ offer })).toBe(offer);
    expect(unwrapRewardOffer(offer)).toBe(offer);
  });

  it('rejects primitive and null select-reward responses', () => {
    expect(unwrapRunFromSelectRewardResponse(null)).toBeNull();
    expect(unwrapRunFromSelectRewardResponse(undefined)).toBeNull();
    expect(unwrapRunFromSelectRewardResponse(false)).toBeNull();
    expect(unwrapRunFromSelectRewardResponse('invalid')).toBeNull();
  });

  it('unwraps valid nested run and rejects malformed nested run variants', () => {
    expect(unwrapRunFromSelectRewardResponse({ run })).toBe(run);
    expect(unwrapRunFromSelectRewardResponse({ run: null })).toBeNull();
    expect(unwrapRunFromSelectRewardResponse({ run: 'invalid' })).toBeNull();
    expect(unwrapRunFromSelectRewardResponse({ run: {} })).toBeNull();
  });

  it('recognizes direct run shape only when id and currentRoom are both present', () => {
    expect(unwrapRunFromSelectRewardResponse(run)).toBe(run);
    expect(unwrapRunFromSelectRewardResponse({ id: 'run-1' })).toBeNull();
    expect(unwrapRunFromSelectRewardResponse({ currentRoom: {} })).toBeNull();
    expect(unwrapRunFromSelectRewardResponse({})).toBeNull();
  });
});
