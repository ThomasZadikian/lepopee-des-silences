import { describe, expect, it, vi, beforeEach } from 'vitest';
import { lawsApi } from './lawsApi';
import { gameEngineApi } from '../../../shared/api/gameEngineApi';

vi.mock('../../../shared/api/gameEngineApi', () => ({
  gameEngineApi: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

describe('lawsApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('listActive sends GET request to the laws catalog route', async () => {
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce({ laws: [] });
    await lawsApi.listActive();
    expect(gameEngineApi.get).toHaveBeenCalledWith('/api/v2/laws');
  });

  it('returns the API response for listActive', async () => {
    const mockResponse = {
      laws: [{
        key: 'law-aegis-v1', name: 'Aegis', description: 'Une loi.', rarity: 'Rare',
        polarity: 'Positif', isMajeure: true, impactDomains: ['Combat'],
      }],
    };
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce(mockResponse);

    const result = await lawsApi.listActive();
    expect(result).toEqual(mockResponse);
  });

  it('propagates errors from gameEngineApi', async () => {
    const error = new Error('Network error');
    vi.mocked(gameEngineApi.get).mockRejectedValueOnce(error);

    await expect(lawsApi.listActive()).rejects.toThrow('Network error');
  });
});
