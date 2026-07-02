import { describe, expect, it, vi, beforeEach } from 'vitest';
import { skillsApi } from './skillsApi';
import { gameEngineApi } from '../../../shared/api/gameEngineApi';

vi.mock('../../../shared/api/gameEngineApi', () => ({
  gameEngineApi: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

describe('skillsApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('listActive sends GET request to the skills route', async () => {
    vi.mocked(gameEngineApi.get).mockResolvedValueOnce({ skills: [] });
    await skillsApi.listActive();
    expect(gameEngineApi.get).toHaveBeenCalledWith('/api/v2/skills');
  });

  it('returns the API response for listActive', async () => {
    const mockResponse = { skills: [{ key: 'skill.basic.strike', displayName: 'Frappe' }] };
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
