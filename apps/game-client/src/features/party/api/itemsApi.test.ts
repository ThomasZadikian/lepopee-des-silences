import { describe, expect, it, vi, beforeEach } from 'vitest';
import { itemsApi } from './itemsApi';
import { gameEngineApi } from '../../../shared/api/gameEngineApi';

vi.mock('../../../shared/api/gameEngineApi', () => ({
  gameEngineApi: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

describe('itemsApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('listActive sends GET request to the items route', async () => {
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce({ items: [] });
    await itemsApi.listActive();
    expect(gameEngineApi.get).toHaveBeenCalledWith('/api/v2/items');
  });

  it('returns the API response for listActive', async () => {
    const mockResponse = { items: [{ key: 'canon.item.monocle-pomenian', displayName: 'Le monocle de Pomenian' }] };
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce(mockResponse);

    const result = await itemsApi.listActive();
    expect(result).toEqual(mockResponse);
  });

  it('propagates errors from gameEngineApi', async () => {
    const error = new Error('Network error');
    vi.mocked(gameEngineApi.get).mockRejectedValueOnce(error);

    await expect(itemsApi.listActive()).rejects.toThrow('Network error');
  });
});
