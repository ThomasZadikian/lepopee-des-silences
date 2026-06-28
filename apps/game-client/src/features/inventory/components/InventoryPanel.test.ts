// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import InventoryPanel from './InventoryPanel.vue';
import type { RunItemDto } from '../types/runTypes';

vi.mock('../../combat/stores/useCombatStore', () => ({
  useCombatStore: vi.fn(() => ({
    allies: [],
    playCombatLogs: vi.fn().mockResolvedValue(undefined),
    finishCombatResponse: vi.fn(),
  })),
}));

vi.mock('../../runs/stores/runStore', () => ({
  useRunStore: vi.fn(() => ({
    loadRun: vi.fn().mockResolvedValue(undefined),
  })),
}));

vi.mock('../api/inventoryApi', () => ({
  inventoryApi: {
    useItem: vi.fn().mockResolvedValue({}),
  },
}));

function mountPanel(items: RunItemDto[], runId = 'run-1') {
  return mount(InventoryPanel, { props: { items, runId } });
}

describe('InventoryPanel', () => {
  const baseItem: RunItemDto = {
    id: 'item-1',
    displayName: 'Potion de soin',
    type: 'Consumable',
    rarity: 'Common',
    description: 'Restaure la vitalité.',
    effectType: 'Heal',
    effectAmount: 20,
    quantity: 1,
    isUsable: true,
  };

  it('renders without crashing', () => {
    const wrapper = mountPanel([]);
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the header', () => {
    const wrapper = mountPanel([]);
    expect(wrapper.text()).toContain('Inventaire de run');
    expect(wrapper.text()).toContain('Sac à dos');
  });

  it('shows empty state when no items', () => {
    const wrapper = mountPanel([]);
    expect(wrapper.text()).toContain('Ton sac est vide');
  });

  it('renders items list', () => {
    const wrapper = mountPanel([baseItem]);
    expect(wrapper.findAll('.inv-item').length).toBe(1);
  });

  it('displays item name', () => {
    const wrapper = mountPanel([baseItem]);
    expect(wrapper.text()).toContain('Potion de soin');
  });

  it('displays rarity chip', () => {
    const wrapper = mountPanel([baseItem]);
    expect(wrapper.text()).toContain('Commun');
  });

  it('displays quantity when > 1', () => {
    const item = { ...baseItem, quantity: 3 };
    const wrapper = mountPanel([item]);
    expect(wrapper.text()).toContain('×3');
  });

  it('displays effect badge', () => {
    const wrapper = mountPanel([baseItem]);
    expect(wrapper.text()).toContain('+20 Vitalité');
  });

  it('shows use button for usable items', () => {
    const wrapper = mountPanel([baseItem]);
    expect(wrapper.text()).toContain('Utiliser');
  });

  it('hides use button for non-usable items', () => {
    const item = { ...baseItem, isUsable: false };
    const wrapper = mountPanel([item]);
    expect(wrapper.text()).not.toContain('Utiliser');
  });

  it('shows ally picker when use is clicked', async () => {
    const wrapper = mountPanel([baseItem]);
    await wrapper.find('.inv-btn--use').trigger('click');
    expect(wrapper.find('.inv-picker').exists()).toBe(true);
  });

  it('shows cancel button in ally picker', async () => {
    const wrapper = mountPanel([baseItem]);
    await wrapper.find('.inv-btn--use').trigger('click');
    expect(wrapper.text()).toContain('Annuler');
  });

  it('cancels use item selection', async () => {
    const wrapper = mountPanel([baseItem]);
    await wrapper.find('.inv-btn--use').trigger('click');
    await wrapper.find('.inv-btn--cancel').trigger('click');
    expect(wrapper.find('.inv-picker').exists()).toBe(false);
  });

  it('applies rarity tone for rare items', () => {
    const item = { ...baseItem, rarity: 'Rare' };
    const wrapper = mountPanel([item]);
    expect(wrapper.text()).toContain('Rare');
    expect(wrapper.find('[class*="frost"]').exists()).toBe(true);
  });

  it('applies rarity tone for epic items', () => {
    const item = { ...baseItem, rarity: 'Epic' };
    const wrapper = mountPanel([item]);
    expect(wrapper.text()).toContain('Épique');
    expect(wrapper.find('[class*="gold"]').exists()).toBe(true);
  });

  it('applies rarity tone for uncommon items', () => {
    const item = { ...baseItem, rarity: 'Uncommon' };
    const wrapper = mountPanel([item]);
    expect(wrapper.text()).toContain('Peu commun');
    expect(wrapper.find('[class*="sap"]').exists()).toBe(true);
  });

  it('shows error message when useItem fails', async () => {
    const { inventoryApi } = await import('../api/inventoryApi');
    vi.mocked(inventoryApi.useItem).mockRejectedValueOnce(new Error('Échec'));

    const wrapper = mountPanel([baseItem]);
    await wrapper.find('.inv-btn--use').trigger('click');
    const confirmBtn = wrapper.find('.inv-ally-btn');
    if (confirmBtn.exists()) await confirmBtn.trigger('click');
    await new Promise((r) => setTimeout(r, 0));

    expect(wrapper.text()).toContain('Échec');
  });

  it('hides effect badge when effectAmount is 0', () => {
    const item = { ...baseItem, effectAmount: 0 };
    const wrapper = mountPanel([item]);
    expect(wrapper.find('.inv-item__effect').exists()).toBe(false);
  });

  it('handles Damage effect type', () => {
    const item = { ...baseItem, effectType: 'Damage', effectAmount: 10 };
    const wrapper = mountPanel([item]);
    expect(wrapper.find('[class*="blood"]').exists()).toBe(true);
  });

  it('displays item type', () => {
    const wrapper = mountPanel([baseItem]);
    expect(wrapper.text()).toContain('Consumable');
  });

  it('displays item description', () => {
    const wrapper = mountPanel([baseItem]);
    expect(wrapper.text()).toContain('Restaure la vitalité.');
  });
});
