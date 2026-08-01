import { describe, expect, it, vi, beforeEach } from 'vitest';
import { cursesApi } from './cursesApi';
import { gameEngineApi } from '../../../shared/api/gameEngineApi';

vi.mock('../../../shared/api/gameEngineApi', () => ({
  gameEngineApi: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

describe('cursesApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('listAvailable sends GET request to the curses catalog route', async () => {
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce({ curses: [] });
    await cursesApi.listAvailable();
    expect(gameEngineApi.get).toHaveBeenCalledWith('/api/v2/curses');
  });

  it('returns the API response for listAvailable', async () => {
    const mockResponse = {
      curses: [{
        key: 'curse.old-wound', displayName: 'Vieille blessure', description: 'Rouvre une plaie.',
        narrativeText: null, severity: 3, duration: 'Permanent', trigger: null,
      }],
    };
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce(mockResponse);

    const result = await cursesApi.listAvailable();
    expect(result).toEqual(mockResponse);
  });

  it('propagates errors from gameEngineApi', async () => {
    const error = new Error('Network error');
    vi.mocked(gameEngineApi.get).mockRejectedValueOnce(error);

    await expect(cursesApi.listAvailable()).rejects.toThrow('Network error');
  });
});
