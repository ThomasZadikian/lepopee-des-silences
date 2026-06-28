import { beforeEach, describe, expect, it, vi } from 'vitest';
import { HttpError } from '../../../shared/api/httpClient';
import { gameEngineApi } from '../../../shared/api/gameEngineApi';
import { combatApi } from './combatApi';
import type { CombatRuntimeDto, UseCombatSkillRequest } from '../types/combatContracts';

vi.mock('../../../shared/api/gameEngineApi', () => ({
  gameEngineApi: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('combatApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getCurrentCombat', () => {
    it('calls the correct endpoint', async () => {
      vi.mocked(gameEngineApi.get).mockResolvedValueOnce({ id: 'combat-1' } as CombatRuntimeDto);
      await combatApi.getCurrentCombat('run-1');
      expect(gameEngineApi.get).toHaveBeenCalledWith('/api/v2/runs/run-1/current-combat');
    });

    it('returns combat data on success', async () => {
      const mockCombat = { id: 'combat-1', status: 'Active' } as CombatRuntimeDto;
      vi.mocked(gameEngineApi.get).mockResolvedValueOnce(mockCombat);
      const result = await combatApi.getCurrentCombat('run-1');
      expect(result).toEqual(mockCombat);
    });

    it('returns null on 404', async () => {
      vi.mocked(gameEngineApi.get).mockRejectedValueOnce(new HttpError('Not Found', 404, null));
      const result = await combatApi.getCurrentCombat('run-1');
      expect(result).toBeNull();
    });

    it('throws on non-404 errors', async () => {
      vi.mocked(gameEngineApi.get).mockRejectedValueOnce(new HttpError('Server Error', 500, null));
      await expect(combatApi.getCurrentCombat('run-1')).rejects.toThrow('Server Error');
    });
  });

  describe('useSkillAction', () => {
    it('calls the correct endpoint', async () => {
      const body: UseCombatSkillRequest = { actorId: 'ally-1', skillKey: 'skill.strike', targetIds: ['enemy-1'] };
      vi.mocked(gameEngineApi.post).mockResolvedValueOnce({} as any);
      await combatApi.useSkillAction('run-1', 'combat-1', body);
      expect(gameEngineApi.post).toHaveBeenCalledWith(
        '/api/v2/runs/run-1/combats/combat-1/skill-actions',
        body,
      );
    });

    it('returns the response', async () => {
      const mockResponse = { accepted: true, message: null };
      vi.mocked(gameEngineApi.post).mockResolvedValueOnce(mockResponse);
      const body: UseCombatSkillRequest = { actorId: 'ally-1', skillKey: 'skill.strike', targetIds: ['enemy-1'] };
      const result = await combatApi.useSkillAction('run-1', 'combat-1', body);
      expect(result).toEqual(mockResponse);
    });
  });

  describe('hold', () => {
    it('calls the correct endpoint with default deltaTicks', async () => {
      vi.mocked(gameEngineApi.post).mockResolvedValueOnce({} as any);
      await combatApi.hold('run-1', 'combat-1');
      expect(gameEngineApi.post).toHaveBeenCalledWith(
        '/api/v2/runs/run-1/combats/combat-1/hold',
        { deltaTicks: 200 },
      );
    });

    it('calls the correct endpoint with custom deltaTicks', async () => {
      vi.mocked(gameEngineApi.post).mockResolvedValueOnce({} as any);
      await combatApi.hold('run-1', 'combat-1', 500);
      expect(gameEngineApi.post).toHaveBeenCalledWith(
        '/api/v2/runs/run-1/combats/combat-1/hold',
        { deltaTicks: 500 },
      );
    });
  });

  describe('advanceCombat', () => {
    it('calls the correct endpoint', async () => {
      vi.mocked(gameEngineApi.post).mockResolvedValueOnce({} as any);
      await combatApi.advanceCombat('run-1', 'combat-1');
      expect(gameEngineApi.post).toHaveBeenCalledWith(
        '/api/v2/runs/run-1/combats/combat-1/advance',
        {},
      );
    });
  });

  describe('useItemAction', () => {
    it('calls the correct endpoint', async () => {
      vi.mocked(gameEngineApi.post).mockResolvedValueOnce({} as any);
      await combatApi.useItemAction('run-1', 'combat-1', { itemId: 'item-1', targetIds: ['ally-1'] });
      expect(gameEngineApi.post).toHaveBeenCalledWith(
        '/api/v2/runs/run-1/combats/combat-1/item-actions',
        { itemId: 'item-1', targetIds: ['ally-1'] },
      );
    });

    it('returns the response', async () => {
      const mockResponse = { accepted: true, combat: { id: 'combat-1' } };
      vi.mocked(gameEngineApi.post).mockResolvedValueOnce(mockResponse);
      const result = await combatApi.useItemAction('run-1', 'combat-1', { itemId: 'item-1', targetIds: ['ally-1'] });
      expect(result).toEqual(mockResponse);
    });
  });
});
