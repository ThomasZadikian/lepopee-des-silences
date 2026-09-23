// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';

import ItemManagementTab from './ItemManagementTab.vue';
import { usePlayerStore } from '../../../party/stores/playerStore';
import { playerApi } from '../../../party/api/playerApi';
import { itemsApi } from '../../../party/api/itemsApi';
import { demoPlayerId } from '../../../runs/stores/runStore';
import type {
  EquipmentChangePlanView, PlayerCharacterView, PlayerProfileView,
} from '../../../party/types/playerTypes';

vi.mock('../../../party/api/playerApi', () => ({
  playerApi: {
    getProfile: vi.fn(),
    equipSkill: vi.fn(),
    unequipSkill: vi.fn(),
    equipItem: vi.fn(),
    unequipItem: vi.fn(),
    previewEquipmentChange: vi.fn(),
    equipItemInstance: vi.fn(),
    unequipItemInstance: vi.fn(),
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
      speed: 10, initiative: 10,focus: 0, mana: 0, charge: 0,
    },
    maxEquippedItems: 3,
    items: [
      { itemInstanceId: 'instance-tome', itemKey: 'item.relic.tome', acquiredAtUtc: '2026-01-01T00:00:00Z', source: 'run', isEquipped: true },
      { itemInstanceId: 'instance-sac', itemKey: 'item.equipment.sac', acquiredAtUtc: '2026-01-01T00:00:00Z', source: 'run', isEquipped: false },
    ],
    ...overrides,
  };
}

function baseProfile(character: PlayerCharacterView): PlayerProfileView {
  return {
    id: 'player-1',
    displayName: 'Test',
    characters: [character],
    progression: { palaceShardCount: 0 },
    permanentItems: [
      { itemInstanceId: 'instance-tome', itemDefinitionKey: 'item.relic.tome', sourceRunId: 'run-1', acquiredAtUtc: '2026-01-01T00:00:00Z' },
      { itemInstanceId: 'instance-sac', itemDefinitionKey: 'item.equipment.sac', sourceRunId: 'run-1', acquiredAtUtc: '2026-01-01T00:00:00Z' },
      { itemInstanceId: 'instance-other', itemDefinitionKey: 'item.never-equipped', sourceRunId: 'run-2', acquiredAtUtc: '2026-01-01T00:00:00Z' },
    ],
  };
}

function equipmentPlan(overrides: Partial<EquipmentChangePlanView> = {}): EquipmentChangePlanView {
  return {
    targetPosition: 'Feet',
    candidateItem: {
      itemInstanceId: 'instance-sac',
      definitionKey: 'item.equipment.sac',
      displayName: 'Bottes du veilleur',
    },
    currentlyEquippedItem: null,
    canEquip: true,
    blockingReasons: [],
    currentEffectiveStats: {
      maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
      speed: 10, initiative: 10, focus: 0, mana: 0, charge: 0, movement: 4,
    },
    projectedEffectiveStats: {
      maxVitality: 100, attackPower: 12, defense: 9, startingGuard: 0,
      speed: 10, initiative: 10, focus: 0, mana: 0, charge: 0, movement: 4,
    },
    statDeltas: [{ stat: 'Defense', current: 6, projected: 9, delta: 3 }],
    currentTemporarySkills: [],
    projectedTemporarySkills: [],
    gainedTemporarySkills: [],
    lostTemporarySkills: [],
    currentVitality: 100,
    projectedCurrentVitality: 100,
    currentMana: 0,
    projectedCurrentMana: 0,
    allowedSlots: ['Feet'],
    proficiencyTags: [],
    ...overrides,
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
          flavorTag: 'Lore', rarity: 'Unique', effectRunType: null, effectValue: 0, equipSlot: 'Relic',
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

    expect(wrapper.find('.imk-profile-header__meta').text()).toContain('1 / 3');
  });

  it('renders the complete paper-doll layout with all authored equipment positions', () => {
    const character = baseCharacter({ archetypeKey: 'archetype.ecrivain' });
    usePlayerStore().profile = baseProfile(character);

    const wrapper = mount(ItemManagementTab, { props: { character } });

    expect(wrapper.find('.imk-paper-doll').exists()).toBe(true);
    expect(wrapper.findAll('[data-equipment-position]')).toHaveLength(14);
    expect(wrapper.find('.imk-avatar-stage').text()).toContain('Le Porteur');
    expect(wrapper.find('.imk-avatar-stage').text()).toContain('Ecrivain');
  });

  it('shows effective character statistics beside the equipment silhouette', () => {
    const character = baseCharacter({
      baseStats: {
        maxVitality: 90, attackPower: 10, defense: 6, startingGuard: 0,
        speed: 10, initiative: 10, focus: 0, mana: 0, charge: 0,
      },
    });
    usePlayerStore().profile = baseProfile(character);

    const wrapper = mount(ItemManagementTab, { props: { character } });
    const stats = wrapper.find('.imk-stat-panel');

    expect(stats.text()).toMatch(/Vitalité100\s*\+10/);
    expect(stats.text()).toMatch(/Attaque12\s*\+2/);
    expect(stats.text()).toContain('Déplacement4');
  });

  it('renders the permanent inventory as an item grid', () => {
    const character = baseCharacter();
    usePlayerStore().profile = baseProfile(character);

    const wrapper = mount(ItemManagementTab, { props: { character } });

    expect(wrapper.find('.imk-inventory-grid').exists()).toBe(true);
    expect(wrapper.findAll('.imk-item-card')).toHaveLength(2);
  });

  it('opens a slot picker on hover and lists only compatible unequipped items', async () => {
    vi.mocked(itemsApi.listActive).mockResolvedValue({
      items: [
        {
          key: 'item.relic.tome', displayName: 'Le Tome 38', description: '', category: 'Relic',
          flavorTag: 'Lore', rarity: 'Unique', effectRunType: null, effectValue: 0,
          allowedSlots: ['Relic'],
        },
        {
          key: 'item.equipment.sac', displayName: 'Bottes du veilleur', description: '', category: 'Armor',
          flavorTag: 'Armure', rarity: 'Rare', effectRunType: null, effectValue: 0,
          allowedSlots: ['Feet'],
        },
      ],
    });
    const character = baseCharacter();
    usePlayerStore().profile = baseProfile(character);
    const wrapper = mount(ItemManagementTab, { props: { character } });
    await flushPromises();

    await wrapper.get('[data-equipment-position="Feet"]').trigger('mouseenter');

    const picker = wrapper.get('.imk-slot-picker');
    expect(picker.text()).toContain('Bottes du veilleur');
    expect(picker.text()).not.toContain('Le Tome 38');
    expect(picker.attributes('aria-label')).toContain('Pieds');
  });

  it('shows the server-authored statistic comparison beside the pointer', async () => {
    vi.mocked(itemsApi.listActive).mockResolvedValue({
      items: [{
        key: 'item.equipment.sac', displayName: 'Bottes du veilleur', description: '', category: 'Armor',
        flavorTag: 'Armure', rarity: 'Rare', effectRunType: null, effectValue: 0,
        allowedSlots: ['Feet'],
      }],
    });
    vi.mocked(playerApi.previewEquipmentChange).mockResolvedValue(equipmentPlan());
    const character = baseCharacter();
    usePlayerStore().profile = baseProfile(character);
    const wrapper = mount(ItemManagementTab, { props: { character } });
    await flushPromises();

    await wrapper.get('[data-equipment-position="Feet"]').trigger('mouseenter');
    const candidate = wrapper.get('.imk-slot-picker__item');
    await candidate.trigger('mouseenter', { clientX: 180, clientY: 220 });
    await flushPromises();

    expect(playerApi.previewEquipmentChange).toHaveBeenCalledWith(
      demoPlayerId, 'char-1', 'instance-sac', 'Feet',
    );
    const comparison = wrapper.get('.imk-comparison-tooltip');
    expect(comparison.text()).toContain('Défense');
    expect(comparison.text()).toContain('6 → 9');
    expect(comparison.text()).toContain('+3');
    expect(comparison.attributes('style')).toContain('left:');

    await candidate.trigger('click');
    await flushPromises();

    expect(wrapper.get('[role="dialog"]').text()).toContain('Bottes du veilleur');
    expect(playerApi.previewEquipmentChange).toHaveBeenCalledTimes(1);
  });

  it('shows the tactical contract of a weapon', async () => {
    vi.mocked(itemsApi.listActive).mockResolvedValue({
      items: [{
        key: 'item.relic.tome',
        displayName: 'Bâton des Silences',
        description: '',
        category: 'Weapon',
        flavorTag: 'Arme',
        rarity: 'Rare',
        equipSlot: 'Weapon',
        effectRunType: null,
        effectValue: 0,
        tacticalRange: 4,
        tacticalAreaShape: 'Single',
        requiresLineOfSight: true,
        basicAttackPower: 12,
        basicAttackCategory: 'Magic',
      }],
    });
    const character = baseCharacter({
      items: [{
        itemKey: 'item.relic.tome',
        acquiredAtUtc: '2026-01-01T00:00:00Z',
        source: 'run',
        isEquipped: true,
        slot: 'Weapon',
      }],
    });
    usePlayerStore().profile = baseProfile(character);

    const wrapper = mount(ItemManagementTab, { props: { character } });
    await flushPromises();

    expect(wrapper.text()).toContain('12 puissance · portée 4 · magique · ligne de vue');
  });

  it('lists only the permanent items owned by the selected character', () => {
    const character = baseCharacter();
    const otherCharacter = baseCharacter({
      id: 'char-2',
      displayName: 'L’autre personnage',
      items: [{
        itemInstanceId: 'instance-other', itemKey: 'item.never-equipped',
        acquiredAtUtc: '2026-01-01T00:00:00Z', source: 'run', isEquipped: false,
      }],
    });
    const profile = baseProfile(character);
    profile.characters = [character, otherCharacter];
    usePlayerStore().profile = profile;
    const wrapper = mount(ItemManagementTab, { props: { character } });

    const backpackSection = wrapper.findAll('.imk-section')[1];
    expect(backpackSection.text()).toContain('item.relic.tome');
    expect(backpackSection.text()).toContain('item.equipment.sac');
    expect(backpackSection.text()).not.toContain('item.never-equipped');
  });

  it('calls unequipItemInstance when clicking an equipped item toggle', async () => {
    const character = baseCharacter();
    usePlayerStore().profile = baseProfile(character);
    const wrapper = mount(ItemManagementTab, { props: { character } });

    const equippedSection = wrapper.findAll('.imk-section')[0];
    await equippedSection.find('.imk-toggle').trigger('click');

    expect(playerApi.unequipItemInstance).toHaveBeenCalledWith(
      demoPlayerId, 'char-1', 'instance-tome',
    );
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
        { itemKey: 'item.d', acquiredAtUtc: '2026-01-01T00:00:00Z', source: 'run', isEquipped: false },
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

  it('does not expose protagonist items in a companion inventory', () => {
    const protagonist = baseCharacter({
      items: [
        { itemKey: 'item.a', acquiredAtUtc: '2026-01-01T00:00:00Z', source: 'run', isEquipped: true },
        { itemKey: 'item.b', acquiredAtUtc: '2026-01-01T00:00:00Z', source: 'run', isEquipped: true },
        { itemKey: 'item.c', acquiredAtUtc: '2026-01-01T00:00:00Z', source: 'run', isEquipped: true },
      ],
    });
    const companion = baseCharacter({
      id: 'char-2',
      displayName: 'Thomas',
      characterType: 'Companion',
      items: [],
    });
    const profile = baseProfile(protagonist);
    profile.characters = [protagonist, companion];
    profile.permanentItems = [
      { itemDefinitionKey: 'item.a', sourceRunId: 'run-1', acquiredAtUtc: '2026-01-01T00:00:00Z' },
      { itemDefinitionKey: 'item.b', sourceRunId: 'run-1', acquiredAtUtc: '2026-01-01T00:00:00Z' },
      { itemDefinitionKey: 'item.c', sourceRunId: 'run-1', acquiredAtUtc: '2026-01-01T00:00:00Z' },
    ];
    usePlayerStore().profile = profile;
    const wrapper = mount(ItemManagementTab, { props: { character: companion } });

    const backpackSection = wrapper.findAll('.imk-section')[1];
    expect(backpackSection.text()).not.toContain('item.a');
  });

  it('shows the empty-backpack message when no permanent items are owned', () => {
    const character = baseCharacter({ items: [] });
    const profile = baseProfile(character);
    profile.permanentItems = [];
    usePlayerStore().profile = profile;
    const wrapper = mount(ItemManagementTab, { props: { character } });

    expect(wrapper.text()).toContain('Ce personnage ne possède encore aucun objet permanent.');
  });
});
