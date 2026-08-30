import { describe, expect, it, vi, beforeEach } from 'vitest';
import { rewardApi } from './rewardApi';
import { gameEngineApi } from '../../../shared/api/gameEngineApi';

vi.mock('../../../shared/api/gameEngineApi', () => ({
  gameEngineApi: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

describe('rewardApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('getPendingReward sends GET request', async () => {
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce({});
    await rewardApi.getPendingReward('run-1');
    expect(gameEngineApi.get).toHaveBeenCalledWith('/api/v2/runs/run-1/rewards/pending');
  });

  it('selectReward sends POST request with body', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await rewardApi.selectReward('run-1', { choiceId: 'c1' });
    expect(gameEngineApi.post).toHaveBeenCalledWith(
      '/api/v2/runs/run-1/rewards/select',
      { choiceId: 'c1' },
    );
  });

  it('returns the API response for getPendingReward', async () => {
    const mockResponse = { offer: { id: 'offer-1', choices: [] } };
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce(mockResponse);

    const result = await rewardApi.getPendingReward('run-1');
    expect(result).toEqual(mockResponse);
  });

  it('returns the API response for selectReward', async () => {
    const mockResponse = { run: { id: 'run-1' } };
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce(mockResponse);

    const result = await rewardApi.selectReward('run-1', { choiceId: 'c1' });
    expect(result).toEqual(mockResponse);
  });

  it('propagates errors from gameEngineApi', async () => {
    const error = new Error('Network error');
    vi.mocked(gameEngineApi.get).mockRejectedValueOnce(error);

    await expect(rewardApi.getPendingReward('run-1')).rejects.toThrow('Network error');
  });
});
