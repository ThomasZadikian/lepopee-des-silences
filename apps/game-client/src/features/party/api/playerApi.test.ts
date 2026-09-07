import { describe, expect, it, vi, beforeEach } from 'vitest';
import { playerApi } from './playerApi';
import { gameEngineApi } from '../../../shared/api/gameEngineApi';

vi.mock('../../../shared/api/gameEngineApi', () => ({
  gameEngineApi: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

describe('playerApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('getProfile sends GET request to the profile route', async () => {
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce({});
    await playerApi.getProfile('player-1');
    expect(gameEngineApi.get).toHaveBeenCalledWith('/api/v2/players/player-1/profile');
  });

  it('gets Palace progress independently from the active Run map', async () => {
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce({});
    await playerApi.getPalaceProgress('player-1');
    expect(gameEngineApi.get).toHaveBeenCalledWith('/api/v2/players/player-1/palace-progress');
  });

  it('equipSkill sends POST request to the equip route', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await playerApi.equipSkill('player-1', 'char-1', 'skill.a');
    expect(gameEngineApi.post).toHaveBeenCalledWith(
      '/api/v2/players/player-1/characters/char-1/skills/skill.a/equip',
    );
  });

  it('unequipSkill sends POST request to the unequip route', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await playerApi.unequipSkill('player-1', 'char-1', 'skill.a');
    expect(gameEngineApi.post).toHaveBeenCalledWith(
      '/api/v2/players/player-1/characters/char-1/skills/skill.a/unequip',
    );
  });

  it('equipItem sends POST request to the item equip route', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await playerApi.equipItem('player-1', 'char-1', 'item.relic.tome');
    expect(gameEngineApi.post).toHaveBeenCalledWith(
      '/api/v2/players/player-1/characters/char-1/items/item.relic.tome/equip',
    );
  });

  it('unequipItem sends POST request to the item unequip route', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});
    await playerApi.unequipItem('player-1', 'char-1', 'item.relic.tome');
    expect(gameEngineApi.post).toHaveBeenCalledWith(
      '/api/v2/players/player-1/characters/char-1/items/item.relic.tome/unequip',
    );
  });

  it('previews and commits equipment by instance and concrete position', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValue({});

    await playerApi.previewEquipmentChange('player-1', 'char-1', 'instance-1', 'Ring2');
    await playerApi.equipItemInstance('player-1', 'char-1', 'instance-1', 'Ring2');
    await playerApi.unequipItemInstance('player-1', 'char-1', 'instance-1');

    expect(gameEngineApi.post).toHaveBeenNthCalledWith(1,
      '/api/v2/players/player-1/characters/char-1/equipment/Ring2/preview/instance-1', {});
    expect(gameEngineApi.post).toHaveBeenNthCalledWith(2,
      '/api/v2/players/player-1/characters/char-1/equipment/Ring2/equip/instance-1', {});
    expect(gameEngineApi.post).toHaveBeenNthCalledWith(3,
      '/api/v2/players/player-1/characters/char-1/equipment/unequip/instance-1');
  });

  it('returns the API response for getProfile', async () => {
    const mockResponse = { id: 'player-1', displayName: 'Test' };
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce(mockResponse);

    const result = await playerApi.getProfile('player-1');
    expect(result).toEqual(mockResponse);
  });

  it('propagates errors from gameEngineApi', async () => {
    const error = new Error('Network error');
    vi.mocked(gameEngineApi.post).mockRejectedValueOnce(error);

    await expect(playerApi.equipSkill('player-1', 'char-1', 'skill.a')).rejects.toThrow('Network error');
  });
});
