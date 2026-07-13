// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';

import ItemManagementTab from './ItemManagementTab.vue';
import { usePlayerStore } from '../../../party/stores/playerStore';
import { playerApi } from '../../../party/api/playerApi';
import { itemsApi } from '../../../party/api/itemsApi';
import { demoPlayerId } from '../../../runs/stores/runStore';
import type { PlayerCharacterView, PlayerProfileView } from '../../../party/types/playerTypes';

vi.mock('../../../party/api/playerApi', () => ({
  playerApi: {
    getProfile: vi.fn(),
    equipSkill: vi.fn(),
    unequipSkill: vi.fn(),
    spendStatPoint: vi.fn(),
    equipItem: vi.fn(),
    unequipItem: vi.fn(),
  },
}));

vi.mock('../../../party/api/itemsApi', () => ({
  itemsApi: {
    listActive: vi.fn(),
  },
}));

function baseCharacter(overrides: Partial<PlayerCharacterView> = {}): PlayerCharacterView {
  return {
    id: 'char-1',
    definitionKey: 'character.player.self',
    displayName: 'Le Porteur',
    maxEquippedSkills: 4,
    characterType: 'Standard',
    skills: [],
    stats: {
      maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
      speed: 10, initiative: 10, recovery: 5, focus: 0, mana: 0, charge: 0,
    },
    maxEquippedItems: 3,
    items: [
      { itemKey: 'item.relic.tome', acquiredAtUtc: '2026-01-01T00:00:00Z', source: 'run', isEquipped: true },
      { itemKey: 'item.equipment.sac', acquiredAtUtc: '2026-01-01T00:00:00Z', source: 'run', isEquipped: false },
    ],
    ...overrides,
  };
}

function baseProfile(character: PlayerCharacterView): PlayerProfileView {
  return {
    id: 'player-1',
    displayName: 'Test',
    characters: [character],
    progression: { unspentStatPoints: 0, totalStatPointsEarned: 0, palaceShardCount: 0 },
    permanentItems: [
      { itemDefinitionKey: 'item.relic.tome', sourceRunId: 'run-1', acquiredAtUtc: '2026-01-01T00:00:00Z' },
      { itemDefinitionKey: 'item.equipment.sac', sourceRunId: 'run-1', acquiredAtUtc: '2026-01-01T00:00:00Z' },
      { itemDefinitionKey: 'item.never-equipped', sourceRunId: 'run-2', acquiredAtUtc: '2026-01-01T00:00:00Z' },
    ],
  };
}

describe('ItemManagementTab', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    vi.mocked(itemsApi.listActive).mockResolvedValue({ items: [] });
  });

  it('resolves the catalog display name instead of the raw item key', async () => {
    vi.mocked(itemsApi.listActive).mockResolvedValue({
      items: [
        {
          key: 'item.relic.tome', displayName: 'Le Tome 38', description: '', category: 'Relic',
          itemType: 'Lore', rarity: 'Unique', effectRunType: null, effectValue: 0,
        },
      ],
    });
    const character = baseCharacter();
    usePlayerStore().profile = baseProfile(character);
    const wrapper = mount(ItemManagementTab, { props: { character } });
    await flushPromises();

    const equippedSection = wrapper.findAll('.imk-section')[0];
    expect(equippedSection.text()).toContain('Le Tome 38');
    expect(equippedSection.text()).not.toContain('item.relic.tome');
  });

  it('shows only equipped items in the first section', () => {
    const character = baseCharacter();
    usePlayerStore().profile = baseProfile(character);
    const wrapper = mount(ItemManagementTab, { props: { character } });

    const equippedSection = wrapper.findAll('.imk-section')[0];
    expect(equippedSection.text()).toContain('item.relic.tome');
    expect(equippedSection.text()).not.toContain('item.equipment.sac');
  });

  it('shows the equipped count against the character cap', () => {
    const character = baseCharacter();
    usePlayerStore().profile = baseProfile(character);
    const wrapper = mount(ItemManagementTab, { props: { character } });

    expect(wrapper.find('.imk-section__title').text()).toContain('1 / 3');
  });

  it('lists every permanently-owned item in the backpack section, regardless of equip state', () => {
    const character = baseCharacter();
    usePlayerStore().profile = baseProfile(character);
    const wrapper = mount(ItemManagementTab, { props: { character } });

    const backpackSection = wrapper.findAll('.imk-section')[1];
    expect(backpackSection.text()).toContain('item.relic.tome');
    expect(backpackSection.text()).toContain('item.equipment.sac');
    expect(backpackSection.text()).toContain('item.never-equipped');
  });

  it('calls unequipItem when clicking an equipped item toggle', async () => {
    const character = baseCharacter();
    usePlayerStore().profile = baseProfile(character);
    const wrapper = mount(ItemManagementTab, { props: { character } });

    const equippedSection = wrapper.findAll('.imk-section')[0];
    await equippedSection.find('.imk-toggle').trigger('click');

    expect(playerApi.unequipItem).toHaveBeenCalledWith(demoPlayerId, 'char-1', 'item.relic.tome');
  });

  it('calls equipItem when clicking an unequipped backpack item toggle', async () => {
    const character = baseCharacter();
    usePlayerStore().profile = baseProfile(character);
    const wrapper = mount(ItemManagementTab, { props: { character } });

    const backpackSection = wrapper.findAll('.imk-section')[1];
    const rows = backpackSection.findAll('.imk-row');
    const sacRow = rows.find((r) => r.text().includes('item.equipment.sac'))!;
    await sacRow.find('.imk-toggle').trigger('click');

    expect(playerApi.equipItem).toHaveBeenCalledWith(demoPlayerId, 'char-1', 'item.equipment.sac');
  });

  it('disables equipping a new item when the loadout is full', () => {
    const character = baseCharacter({
      items: [
        { itemKey: 'item.a', acquiredAtUtc: '2026-01-01T00:00:00Z', source: 'run', isEquipped: true },
        { itemKey: 'item.b', acquiredAtUtc: '2026-01-01T00:00:00Z', source: 'run', isEquipped: true },
        { itemKey: 'item.c', acquiredAtUtc: '2026-01-01T00:00:00Z', source: 'run', isEquipped: true },
      ],
    });
    const profile = baseProfile(character);
    profile.permanentItems = [
      { itemDefinitionKey: 'item.a', sourceRunId: 'run-1', acquiredAtUtc: '2026-01-01T00:00:00Z' },
      { itemDefinitionKey: 'item.b', sourceRunId: 'run-1', acquiredAtUtc: '2026-01-01T00:00:00Z' },
      { itemDefinitionKey: 'item.c', sourceRunId: 'run-1', acquiredAtUtc: '2026-01-01T00:00:00Z' },
      { itemDefinitionKey: 'item.d', sourceRunId: 'run-1', acquiredAtUtc: '2026-01-01T00:00:00Z' },
    ];
    usePlayerStore().profile = profile;
    const wrapper = mount(ItemManagementTab, { props: { character } });

    const backpackSection = wrapper.findAll('.imk-section')[1];
    const rows = backpackSection.findAll('.imk-row');
    const dRow = rows.find((r) => r.text().includes('item.d'))!;
    expect(dRow.find('.imk-toggle').attributes('disabled')).toBeDefined();
  });

  it('shows the empty-backpack message when no permanent items are owned', () => {
    const character = baseCharacter({ items: [] });
    const profile = baseProfile(character);
    profile.permanentItems = [];
    usePlayerStore().profile = profile;
    const wrapper = mount(ItemManagementTab, { props: { character } });

    expect(wrapper.text()).toContain('Le sac permanent est vide pour l\'instant.');
  });
});
