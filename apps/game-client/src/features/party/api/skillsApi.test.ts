import { describe, expect, it, vi, beforeEach } from 'vitest';
import { skillsApi } from './skillsApi';
import { gameEngineApi } from '../../../shared/api/gameEngineApi';
import { useEmotionalRegisterCatalog } from '../../emotional-registers/store';

import type { SkillDefinitionView } from '../types/skillTypes';

vi.mock('../../../shared/api/gameEngineApi', () => ({
  gameEngineApi: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

describe('skillsApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useEmotionalRegisterCatalog().install('test', [{
      code: 'neutral',
      displayName: 'Neutre',
      glyph: '·',
      color: 'gray',
      incomingAffinities: [{ incomingRegister: 'neutral', outcome: 'Neutral', multiplier: 1 }],
    }]);
  });

  it('listActive sends GET request to the skills route', async () => {
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce({ skills: [] });
    await skillsApi.listActive();
    expect(gameEngineApi.get).toHaveBeenCalledWith('/api/v2/skills');
  });

  it('returns the API response for listActive', async () => {
    const skill = {
      key: 'skill.basic.strike',
      displayName: 'Frappe',
      description: 'Une attaque simple.',
      skillType: 'Damage',
      targetingType: 'SingleEnemy',
      effectType: 'Damage',
      manaCost: 0,
      chargeCost: 0,
      basePower: 10,
      category: 'Basic',
      basePowerIsPercentOfMaxVitality: false,
      effects: [],
      acquisitionHints: [],
      emotionalRegister: 'neutral',
      compatibleCharacterDefinitionKeys: [],
    } satisfies SkillDefinitionView;
    const mockResponse = { skills: [skill] };
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce(mockResponse);

    const result = await skillsApi.listActive();
    expect(result).toEqual(mockResponse);
  });

  it('propagates errors from gameEngineApi', async () => {
    const error = new Error('Network error');
    vi.mocked(gameEngineApi.get).mockRejectedValueOnce(error);

    await expect(skillsApi.listActive()).rejects.toThrow('Network error');
  });
});
