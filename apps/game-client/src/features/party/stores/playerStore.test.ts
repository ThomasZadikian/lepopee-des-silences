import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

import { playerApi } from '../api/playerApi';
import { usePlayerStore } from './playerStore';
import type { PlayerProfileView } from '../types/playerTypes';
import { demoPlayerId } from '../../runs/stores/runStore';

vi.mock('../api/playerApi', () => ({
  playerApi: {
    getProfile: vi.fn(),
    equipSkill: vi.fn(),
    unequipSkill: vi.fn(),
    spendStatPoint: vi.fn(),
    equipItem: vi.fn(),
    unequipItem: vi.fn(),
  },
}));

function baseProfile(overrides: Partial<PlayerProfileView['characters'][0]> = {}): PlayerProfileView {
  return {
    id: 'player-1',
    displayName: 'Test Player',
    characters: [
      {
        id: 'char-1',
        definitionKey: 'character.player.self',
        displayName: 'Le Porteur',
        maxEquippedSkills: 4,
        items: [],
        maxEquippedItems: 3,
        characterType: 'Standard',
        skills: [
          { skillKey: 'skill.a', unlockedAtUtc: '2026-01-01T00:00:00Z', source: 'default', isEquipped: true },
          { skillKey: 'skill.b', unlockedAtUtc: '2026-01-01T00:00:00Z', source: 'default', isEquipped: false },
        ],
        stats: {
          maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
          speed: 10, initiative: 10,focus: 0, mana: 0, charge: 0,
        },
        ...overrides,
      },
    ],
    progression: { unspentStatPoints: 2, totalStatPointsEarned: 3, palaceShardCount: 0 },
    permanentItems: [],
  };
}

describe('usePlayerStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('starts with no profile loaded', () => {
    const store = usePlayerStore();
    expect(store.profile).toBeNull();
    expect(store.mainCharacter).toBeNull();
  });

  it('loads the profile and exposes the main character', async () => {
    vi.mocked(playerApi.getProfile).mockResolvedValue(baseProfile());
    const store = usePlayerStore();

    await store.loadProfile(demoPlayerId);

    expect(store.mainCharacter?.id).toBe('char-1');
    expect(store.error).toBeNull();
  });

  it('exposes unspentStatPoints from progression', async () => {
    vi.mocked(playerApi.getProfile).mockResolvedValue(baseProfile());
    const store = usePlayerStore();

    await store.loadProfile(demoPlayerId);

    expect(store.unspentStatPoints).toBe(2);
  });

  it('equipSkill replaces the profile with the API response', async () => {
    vi.mocked(playerApi.getProfile).mockResolvedValue(baseProfile());
    const updated = baseProfile();
    updated.characters[0].skills[1].isEquipped = true;
    vi.mocked(playerApi.equipSkill).mockResolvedValue(updated);
    const store = usePlayerStore();
    await store.loadProfile(demoPlayerId);

    await store.equipSkill('char-1', 'skill.b');

    expect(playerApi.equipSkill).toHaveBeenCalledWith(demoPlayerId, 'char-1', 'skill.b');
    expect(store.profile?.characters[0].skills[1].isEquipped).toBe(true);
  });

  it('spendStatPoint replaces the profile and decrements unspent points', async () => {
    vi.mocked(playerApi.getProfile).mockResolvedValue(baseProfile());
    const updated = baseProfile();
    updated.progression.unspentStatPoints = 1;
    vi.mocked(playerApi.spendStatPoint).mockResolvedValue(updated);
    const store = usePlayerStore();
    await store.loadProfile(demoPlayerId);

    await store.spendStatPoint('char-1', 'AttackPower');

    expect(playerApi.spendStatPoint).toHaveBeenCalledWith(demoPlayerId, 'char-1', 'AttackPower');
    expect(store.unspentStatPoints).toBe(1);
  });

  it('sets an error message when an action fails', async () => {
    vi.mocked(playerApi.getProfile).mockRejectedValue(new Error('network down'));
    const store = usePlayerStore();

    await store.loadProfile(demoPlayerId);

    expect(store.error).toBe('network down');
    expect(store.profile).toBeNull();
  });

  it('exposes permanentItems from the profile', async () => {
    const profile = baseProfile();
    profile.permanentItems = [
      { itemDefinitionKey: 'item.relic.tome', sourceRunId: 'run-1', acquiredAtUtc: '2026-01-01T00:00:00Z' },
    ];
    vi.mocked(playerApi.getProfile).mockResolvedValue(profile);
    const store = usePlayerStore();

    await store.loadProfile(demoPlayerId);

    expect(store.permanentItems).toHaveLength(1);
    expect(store.permanentItems[0].itemDefinitionKey).toBe('item.relic.tome');
  });

  it('equipItem replaces the profile with the API response', async () => {
    vi.mocked(playerApi.getProfile).mockResolvedValue(baseProfile());
    const updated = baseProfile({
      items: [{ itemKey: 'item.a', acquiredAtUtc: '2026-01-01T00:00:00Z', source: null, isEquipped: true }],
    });
    vi.mocked(playerApi.equipItem).mockResolvedValue(updated);
    const store = usePlayerStore();
    await store.loadProfile(demoPlayerId);

    await store.equipItem('char-1', 'item.a');

    expect(playerApi.equipItem).toHaveBeenCalledWith(demoPlayerId, 'char-1', 'item.a');
    expect(store.profile?.characters[0].items[0].isEquipped).toBe(true);
  });

  it('unequipItem replaces the profile with the API response', async () => {
    const initial = baseProfile({
      items: [{ itemKey: 'item.a', acquiredAtUtc: '2026-01-01T00:00:00Z', source: null, isEquipped: true }],
    });
    vi.mocked(playerApi.getProfile).mockResolvedValue(initial);
    const updated = baseProfile({
      items: [{ itemKey: 'item.a', acquiredAtUtc: '2026-01-01T00:00:00Z', source: null, isEquipped: false }],
    });
    vi.mocked(playerApi.unequipItem).mockResolvedValue(updated);
    const store = usePlayerStore();
    await store.loadProfile(demoPlayerId);

    await store.unequipItem('char-1', 'item.a');

    expect(playerApi.unequipItem).toHaveBeenCalledWith(demoPlayerId, 'char-1', 'item.a');
    expect(store.profile?.characters[0].items[0].isEquipped).toBe(false);
  });
});
