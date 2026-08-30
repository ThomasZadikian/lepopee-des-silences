import { describe, expect, it, vi, beforeEach } from 'vitest';
import { reputationApi } from './reputationApi';
import { gameEngineApi } from '../../../shared/api/gameEngineApi';

vi.mock('../../../shared/api/gameEngineApi', () => ({
  gameEngineApi: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

describe('reputationApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('getRunReputation sends GET request to the run reputation route', async () => {
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce({ runId: 'run-1', npcs: [] });
    await reputationApi.getRunReputation('run-1');
    expect(gameEngineApi.get).toHaveBeenCalledWith('/api/v2/runs/run-1/reputation');
  });

  it('returns the API response for getRunReputation', async () => {
    const mockResponse = { runId: 'run-1', npcs: [{ npcKey: 'npc.erina', displayName: 'Erina' }] };
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce(mockResponse);

    const result = await reputationApi.getRunReputation('run-1');
    expect(result).toEqual(mockResponse);
  });

  it('propagates errors from gameEngineApi', async () => {
    const error = new Error('Network error');
    vi.mocked(gameEngineApi.get).mockRejectedValueOnce(error);

    await expect(reputationApi.getRunReputation('run-1')).rejects.toThrow('Network error');
  });
});
