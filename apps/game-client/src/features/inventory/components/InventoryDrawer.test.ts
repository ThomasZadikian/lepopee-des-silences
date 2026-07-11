// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import InventoryDrawer from './InventoryDrawer.vue';
import type { RunItemDto } from '../types/runTypes';

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

vi.mock('../../party/api/itemsApi', () => ({
  itemsApi: {
    listActive: vi.fn().mockResolvedValue({ items: [] }),
  },
}));

function mountDrawer(items: RunItemDto[], runId = 'run-1', capacity?: number | null) {
  return mount(InventoryDrawer, {
    props: { items, runId, capacity },
    global: {
      stubs: {
        Transition: { template: '<slot />' },
      },
    },
  });
}

describe('InventoryDrawer', () => {
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
    const wrapper = mountDrawer([]);
    expect(wrapper.exists()).toBe(true);
  });

  it('emits close when close button is clicked', async () => {
    const wrapper = mountDrawer([]);
    await wrapper.find('.bsd-close').trigger('click');
    expect(wrapper.emitted('close')).toHaveLength(1);
  });

  it('shows empty state when no items', () => {
    const wrapper = mountDrawer([]);
    expect(wrapper.text()).toContain('Ton sac est vide.');
  });

  it('renders items in grid', () => {
    const wrapper = mountDrawer([baseItem]);
    expect(wrapper.findAll('.bsd-cell').length).toBe(1);
  });

  it('displays item name', () => {
    const wrapper = mountDrawer([baseItem]);
    expect(wrapper.text()).toContain('Potion de soin');
  });

  it('displays rarity label', () => {
    const wrapper = mountDrawer([baseItem]);
    expect(wrapper.text()).toContain('Commun');
  });

  it('displays quantity when > 1', () => {
    const item = { ...baseItem, quantity: 3 };
    const wrapper = mountDrawer([item]);
    expect(wrapper.text()).toContain('×3');
  });

  it('selects an item on click', async () => {
    const wrapper = mountDrawer([baseItem]);
    await wrapper.find('.bsd-cell').trigger('click');
    expect(wrapper.find('.bsd-cell--sel').exists()).toBe(true);
  });

  it('shows item detail sheet when selected', async () => {
    const wrapper = mountDrawer([baseItem]);
    await wrapper.find('.bsd-cell').trigger('click');
    expect(wrapper.find('.bsd-sheet').exists()).toBe(true);
    expect(wrapper.text()).toContain('Potion de soin');
  });

  it('displays effect label in sheet', async () => {
    const wrapper = mountDrawer([baseItem]);
    await wrapper.find('.bsd-cell').trigger('click');
    expect(wrapper.text()).toContain('+20 Vitalité');
  });

  it('shows use button for usable items', async () => {
    const wrapper = mountDrawer([baseItem]);
    await wrapper.find('.bsd-cell').trigger('click');
    expect(wrapper.text()).toContain('Utiliser');
  });

  it('shows the "Lire" button for a readable item', async () => {
    const { itemsApi } = await import('../../party/api/itemsApi');
    vi.mocked(itemsApi.listActive).mockResolvedValueOnce({
      items: [{
        key: 'canon.item.carnet',
        displayName: 'Le carnet',
        description: 'Un carnet.',
        category: 'Relic',
        itemType: 'Lore',
        rarity: 'Unique',
        effectRunType: null,
        effectValue: 0,
        readablePages: ['PLACEHOLDER HISTOIRE 01'],
      }],
    });

    const item = { ...baseItem, definitionKey: 'canon.item.carnet', isUsable: false };
    const wrapper = mountDrawer([item]);
    await new Promise((r) => setTimeout(r, 0));
    await wrapper.find('.bsd-cell').trigger('click');

    expect(wrapper.find('.bsd-action-btn--read').exists()).toBe(true);
  });

  it('does not show the "Lire" button for a non-readable item', async () => {
    const wrapper = mountDrawer([{ ...baseItem, definitionKey: 'canon.item.potion' }]);
    await new Promise((r) => setTimeout(r, 0));
    await wrapper.find('.bsd-cell').trigger('click');

    expect(wrapper.find('.bsd-action-btn--read').exists()).toBe(false);
  });

  it('shows unusable message for non-usable items', async () => {
    const item = { ...baseItem, isUsable: false };
    const wrapper = mountDrawer([item]);
    await wrapper.find('.bsd-cell').trigger('click');
    expect(wrapper.text()).toContain('Cet objet ne peut pas être utilisé');
  });

  it('applies rarity tone classes', () => {
    const item = { ...baseItem, rarity: 'Rare' };
    const wrapper = mountDrawer([item]);
    expect(wrapper.text()).toContain('Rare');
    expect(wrapper.find('[class*="frost"]').exists()).toBe(true);
  });

  it('applies epic rarity tone', () => {
    const item = { ...baseItem, rarity: 'Epic' };
    const wrapper = mountDrawer([item]);
    expect(wrapper.text()).toContain('Épique');
    expect(wrapper.find('[class*="gold"]').exists()).toBe(true);
  });

  it('applies uncommon rarity tone', () => {
    const item = { ...baseItem, rarity: 'Uncommon' };
    const wrapper = mountDrawer([item]);
    expect(wrapper.text()).toContain('Peu commun');
    expect(wrapper.find('[class*="sap"]').exists()).toBe(true);
  });

  it('shows error message when useItem fails', async () => {
    const { inventoryApi } = await import('../api/inventoryApi');
    vi.mocked(inventoryApi.useItem).mockRejectedValueOnce(new Error('Échec'));

    const wrapper = mountDrawer([baseItem]);
    await wrapper.find('.bsd-cell').trigger('click');
    await wrapper.find('.bsd-action-btn--use').trigger('click');
    await new Promise((r) => setTimeout(r, 0));

    expect(wrapper.text()).toContain('Échec');
  });

  it('hides effect section when effectAmount is 0', async () => {
    const item = { ...baseItem, effectAmount: 0 };
    const wrapper = mountDrawer([item]);
    await wrapper.find('.bsd-cell').trigger('click');
    expect(wrapper.find('.bsd-sheet__effect').exists()).toBe(false);
  });

  it('clears selection after successful use', async () => {
    const wrapper = mountDrawer([baseItem]);
    await wrapper.find('.bsd-cell').trigger('click');
    await wrapper.find('.bsd-action-btn--use').trigger('click');
    await new Promise((r) => setTimeout(r, 0));
    expect(wrapper.find('.bsd-sheet').exists()).toBe(false);
  });

  it('handles Guard effect type', async () => {
    const item = { ...baseItem, effectType: 'Guard', effectAmount: 10 };
    const wrapper = mountDrawer([item]);
    await wrapper.find('.bsd-cell').trigger('click');
    expect(wrapper.text()).toContain('+10 Garde');
  });

  it('handles ManaRestore effect type', async () => {
    const item = { ...baseItem, effectType: 'ManaRestore', effectAmount: 5 };
    const wrapper = mountDrawer([item]);
    await wrapper.find('.bsd-cell').trigger('click');
    expect(wrapper.text()).toContain('+5 Mana');
  });

  it('handles ChargeRestore effect type', async () => {
    const item = { ...baseItem, effectType: 'ChargeRestore', effectAmount: 3 };
    const wrapper = mountDrawer([item]);
    await wrapper.find('.bsd-cell').trigger('click');
    expect(wrapper.text()).toContain('+3 Charge');
  });

  it('handles NextCombatGuard effect type', async () => {
    const item = { ...baseItem, effectType: 'NextCombatGuard', effectAmount: 15 };
    const wrapper = mountDrawer([item]);
    await wrapper.find('.bsd-cell').trigger('click');
    expect(wrapper.text()).toContain('+15 Garde (prochain combat)');
  });

  it('handles NarrativeFragment effect type', async () => {
    const item = { ...baseItem, effectType: 'NarrativeFragment', effectAmount: 1 };
    const wrapper = mountDrawer([item]);
    await wrapper.find('.bsd-cell').trigger('click');
    expect(wrapper.text()).toContain('Fragment narratif');
  });

  it('shows the capacity counter when capacity is provided', () => {
    const wrapper = mountDrawer([baseItem], 'run-1', 6);
    expect(wrapper.text()).toContain('1 / 6');
  });

  it('hides the capacity counter when capacity is not provided', () => {
    const wrapper = mountDrawer([baseItem]);
    expect(wrapper.find('.bsd-capacity').exists()).toBe(false);
  });

  it('flags the capacity counter as full when the bag has reached capacity', () => {
    const items = [baseItem, { ...baseItem, id: 'item-2' }];
    const wrapper = mountDrawer(items, 'run-1', 2);
    expect(wrapper.find('.bsd-capacity--full').exists()).toBe(true);
  });
});
