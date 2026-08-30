import { beforeEach, describe, expect, it, vi } from 'vitest';

import { gameEngineApi } from '../../../shared/api/gameEngineApi';
import { combatApi } from './combatApi';

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

  it('sends an explicit vitality-sacrifice confirmation with a tactical skill', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});

    await combatApi.useTacticalSkill('run-1', 'skill.ultimate', 4, 2, true);

    expect(gameEngineApi.post).toHaveBeenCalledWith(
      '/api/v2/runs/run-1/tactical-combat/skill',
      {
        skillKey: 'skill.ultimate',
        targetX: 4,
        targetY: 2,
        confirmVitalitySacrifice: true,
      },
    );
  });

  it('does not confirm a sacrifice implicitly', async () => {
    vi.mocked(gameEngineApi.post).mockResolvedValueOnce({});

    await combatApi.useTacticalSkill('run-1', 'skill.basic.strike', 1, 1);

    expect(gameEngineApi.post).toHaveBeenCalledWith(
      '/api/v2/runs/run-1/tactical-combat/skill',
      expect.objectContaining({ confirmVitalitySacrifice: false }),
    );
  });
});
