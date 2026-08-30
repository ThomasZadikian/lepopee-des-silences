import { describe, expect, it, vi, beforeEach } from 'vitest';
import { inventoryApi } from './inventoryApi';
import { gameEngineApi } from '../../../shared/api/gameEngineApi';

vi.mock('../../../shared/api/gameEngineApi', () => ({
  gameEngineApi: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

describe('inventoryApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('getRunInventory sends GET request', async () => {
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce({});
    await inventoryApi.getRunInventory('run-1');
    expect(gameEngineApi.get).toHaveBeenCalledWith('/api/v2/runs/run-1/inventory');
  });

  it('useItem sends POST request', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await inventoryApi.useItem('run-1', 'item-1');
    expect(gameEngineApi.post).toHaveBeenCalledWith('/api/v2/runs/run-1/inventory/item-1/use');
  });

  it('readGrimoire sends the selected character', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});

    await inventoryApi.readGrimoire('run-1', 'grimoire-1', 'character-1');

    expect(gameEngineApi.post).toHaveBeenCalledWith(
      '/api/v2/runs/run-1/inventory/grimoire-1/read-grimoire',
      { characterId: 'character-1' },
    );
  });

  it('returns the API response for getRunInventory', async () => {
    const mockResponse = { items: [{ id: 'item-1' }] };
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce(mockResponse);

    const result = await inventoryApi.getRunInventory('run-1');
    expect(result).toEqual(mockResponse);
  });

  it('returns the API response for useItem', async () => {
    const mockResponse = { usedInCombat: false };
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce(mockResponse);

    const result = await inventoryApi.useItem('run-1', 'item-1');
    expect(result).toEqual(mockResponse);
  });

  it('propagates errors from gameEngineApi', async () => {
    const error = new Error('Network error');
    vi.mocked(gameEngineApi.get).mockRejectedValueOnce(error);

    await expect(inventoryApi.getRunInventory('run-1')).rejects.toThrow('Network error');
  });
});
