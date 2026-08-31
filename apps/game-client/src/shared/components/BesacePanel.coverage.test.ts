import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import BesacePanel from './BesacePanel.vue';
import { inventoryApi } from '../../features/inventory/api/inventoryApi';
import { itemsApi } from '../../features/party/api/itemsApi';

const mocks = vi.hoisted(() => ({
  runStore: {
    currentRun: null as any,
    shouldShowCombatScene: false,
    loadRun: vi.fn(),
    grantPermanentItem: vi.fn(),
    syncPartyStats: vi.fn(),
  },
  playerStore: {
    permanentItems: [] as any[],
    profile: null as any,
    isLoading: false,
    loadProfile: vi.fn(),
    equipItem: vi.fn(),
    unequipItem: vi.fn(),
  },
}));

vi.mock('../../features/runs/stores/runStore', () => ({
  useRunStore: () => mocks.runStore,
}));

vi.mock('../../features/party/stores/playerStore', () => ({
  usePlayerStore: () => mocks.playerStore,
}));

vi.mock('../../features/inventory/api/inventoryApi', () => ({
  inventoryApi: {
    useItem: vi.fn(),
    readGrimoire: vi.fn(),
  },
}));

vi.mock('../../features/party/api/itemsApi', () => ({
  itemsApi: { listActive: vi.fn() },
}));

vi.mock('../theme/typeColors', () => ({
  itemTypeMeta: (type?: string | null) => ({ label: type ?? 'Objet', glyph: '*', color: 'type-color' }),
  itemRarityMeta: (rarity?: string | null) => ({ label: rarity ?? 'Common', glyph: '·', color: 'rarity-color' }),
  itemEffectTypeMeta: (effect?: string | null) => ({ label: effect ?? 'Effet', glyph: '+', color: 'effect-color' }),
}));

function item(overrides: Record<string, unknown> = {}) {
  return {
    id: `item-${Math.random()}`,
    definitionKey: 'item.default',
    displayName: 'Objet',
    description: 'Description',
    type: 'Misc',
    rarity: 'Common',
    quantity: 1,
    isUsable: false,
    effectType: 'None',
    effectAmount: 0,
    tacticalRange: undefined,
    tacticalAreaShape: undefined,
    requiresLineOfSight: false,
    equipSlot: undefined,
    ...overrides,
  } as any;
}

function findCell(wrapper: ReturnType<typeof mount>, name: string) {
  const cell = wrapper.findAll('.bp-cell').find((candidate) => candidate.text().includes(name));
  if (!cell) throw new Error(`Cell ${name} not found`);
  return cell;
}

function mountPanel(items: any[], capacity: number | null | undefined = 10) {
  return mount(BesacePanel, {
    props: { items, runId: 'run-1', capacity },
    global: {
      stubs: {
        BookReader: {
          props: ['modelValue', 'title', 'pages'],
          template: '<div data-test="reader">{{ title }}:{{ pages?.length }}</div>',
        },
      },
    },
  });
}

describe('BesacePanel coverage margin', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.runStore.currentRun = {
      id: 'run-1',
      party: {
        members: [
          {
            id: 'char-1',
            displayName: 'Thomas',
            skills: [{ displayName: 'Ancien grimoire', temporarySlot: 'Grimoire' }],
          },
        ],
      },
    };
    mocks.runStore.shouldShowCombatScene = false;
    mocks.playerStore.permanentItems = [];
    mocks.playerStore.profile = {
      characters: [{ id: 'char-1', items: [] }],
    };
    mocks.playerStore.isLoading = false;
    vi.mocked(itemsApi.listActive).mockResolvedValue({ items: [] } as any);
    vi.mocked(inventoryApi.useItem).mockResolvedValue({} as any);
    vi.mocked(inventoryApi.readGrimoire).mockResolvedValue({} as any);
  });

  it('groups every category and covers effect, tactical, value and readable fallbacks', async () => {
    vi.mocked(itemsApi.listActive).mockResolvedValue({
      items: [
        { key: 'weapon', readablePages: [], palaceShardCost: 4, himLitShardCost: 2 },
        { key: 'book', readablePages: ['p1', 'p2'], palaceShardCost: null, himLitShardCost: null },
        { key: 'free', readablePages: null, palaceShardCost: 0, himLitShardCost: 0 },
      ],
    } as any);
    const items = [
      item({ id: 'w', definitionKey: 'weapon', displayName: 'Arme', equipSlot: 'Weapon', type: 'Weapon', quantity: 2, effectType: 'HealthPercent', effectAmount: 25, tacticalRange: 3, tacticalAreaShape: 'Cross', requiresLineOfSight: true }),
      item({ id: 'a', definitionKey: 'accessory', displayName: 'Accessoire', equipSlot: 'Accessory', type: 'Equipment', effectType: 'NarrativeFragment', effectAmount: 0, tacticalRange: 2, tacticalAreaShape: 'Diamond', requiresLineOfSight: false }),
      item({ id: 'r', definitionKey: 'relic', displayName: 'Relique', equipSlot: 'Relic', type: 'Relic', tacticalRange: 9, tacticalAreaShape: 'Map' }),
      item({ id: 'c', definitionKey: 'consumable', displayName: 'Soin', type: 'Consumable', isUsable: true, effectType: 'Heal', effectAmount: 10, tacticalRange: 1 }),
      item({ id: 'o', definitionKey: 'free', displayName: 'Autre', type: 'Misc', effectType: 'Heal', effectAmount: 0 }),
      item({ id: 'b', definitionKey: 'book', displayName: 'Livre', type: 'Book' }),
    ];
    const wrapper = mountPanel(items, items.length);
    await flushPromises();

    expect(wrapper.text()).toContain('Accessoires');
    expect(wrapper.text()).toContain('Armes');
    expect(wrapper.text()).toContain('Objets de soin');
    expect(wrapper.text()).toContain('Autres');
    expect(wrapper.find('.bp-capacity').classes()).toContain('bp-capacity--full');

    await findCell(wrapper, 'Arme').trigger('click');
    expect(wrapper.text()).toContain('+25% HealthPercent');
    expect(wrapper.text()).toContain('croix (rayon 1)');
    expect(wrapper.text()).toContain('ligne de vue requise');
    expect(wrapper.text()).toContain("4 éclats · 2 éclats de Him'Lit");

    await findCell(wrapper, 'Accessoire').trigger('click');
    expect(wrapper.text()).toContain('NarrativeFragment');
    expect(wrapper.text()).toContain('losange (rayon 2)');
    expect(wrapper.text()).toContain('ignore la ligne de vue');

    await findCell(wrapper, 'Relique').trigger('click');
    expect(wrapper.text()).toContain('carte entière');
    expect(wrapper.find('.bp-sheet__effect').exists()).toBe(false);

    await findCell(wrapper, 'Soin').trigger('click');
    expect(wrapper.text()).toContain('+10 Heal');
    expect(wrapper.text()).toContain('cible unique');

    await findCell(wrapper, 'Autre').trigger('click');
    expect(wrapper.find('.bp-sheet__effect').exists()).toBe(false);
    expect(wrapper.find('.bp-sheet__contract').exists()).toBe(false);
    expect(wrapper.find('.bp-sheet__value').exists()).toBe(false);

    await findCell(wrapper, 'Livre').trigger('click');
    expect(wrapper.find('.bp-btn').text()).toBe('Lire');
    await wrapper.find('.bp-btn').trigger('click');
    expect(wrapper.find('[data-test="reader"]').exists()).toBe(true);

    const firstGroup = wrapper.find('.bp-group-head');
    expect(wrapper.findAll('.bp-grid').length).toBeGreaterThan(0);
    await firstGroup.trigger('click');
    expect(firstGroup.text()).toContain('▸');
    await firstGroup.trigger('click');
    expect(firstGroup.text()).toContain('▾');
  });

  it('uses consumables successfully and exposes both error families', async () => {
    const usable = item({ id: 'use', definitionKey: 'use', displayName: 'Potion', type: 'Consumable', isUsable: true });
    const wrapper = mountPanel([usable], undefined);
    await flushPromises();
    expect(wrapper.text()).not.toContain('/ 10');

    await findCell(wrapper, 'Potion').trigger('click');
    await wrapper.find('.bp-btn').trigger('click');
    await flushPromises();
    expect(inventoryApi.useItem).toHaveBeenCalledWith('run-1', 'use');
    expect(mocks.runStore.loadRun).toHaveBeenCalledWith('run-1');
    expect(wrapper.find('.bp-sheet').exists()).toBe(false);

    vi.mocked(inventoryApi.useItem).mockRejectedValueOnce(new Error('Impossible'));
    await findCell(wrapper, 'Potion').trigger('click');
    await wrapper.find('.bp-btn').trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('Impossible');

    vi.mocked(inventoryApi.useItem).mockRejectedValueOnce('bad');
    await wrapper.find('.bp-btn').trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain("L'utilisation a échoué.");
  });

  it('reads a grimoire, shows replacement warning, and handles read errors', async () => {
    const grimoire = item({ id: 'grim', definitionKey: 'grim', displayName: 'Grimoire', type: 'Grimoire' });
    const wrapper = mountPanel([grimoire]);
    await flushPromises();

    await findCell(wrapper, 'Grimoire').trigger('click');
    expect(wrapper.text()).toContain('Remplace Ancien grimoire');
    expect(wrapper.find('select').element.value).toBe('char-1');

    await wrapper.find('.bp-btn').trigger('click');
    await flushPromises();
    expect(inventoryApi.readGrimoire).toHaveBeenCalledWith('run-1', 'grim', 'char-1');
    expect(mocks.runStore.loadRun).toHaveBeenCalled();

    vi.mocked(inventoryApi.readGrimoire).mockRejectedValueOnce(new Error('Lecture interdite'));
    await findCell(wrapper, 'Grimoire').trigger('click');
    await wrapper.find('.bp-btn').trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('Lecture interdite');

    vi.mocked(inventoryApi.readGrimoire).mockRejectedValueOnce('bad');
    await wrapper.find('.bp-btn').trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('La lecture a échoué.');
  });

  it('grants and equips a run-found item, synchronizes stats, then covers unequip and full-slot states', async () => {
    const weapon = item({ id: 'weapon', definitionKey: 'weapon', displayName: 'Lame', equipSlot: 'Weapon', type: 'Weapon' });
    const wrapper = mountPanel([weapon]);
    await flushPromises();

    await findCell(wrapper, 'Lame').trigger('click');
    expect(wrapper.text()).toContain('sac permanent');
    await wrapper.find('.bp-btn').trigger('click');
    await flushPromises();
    expect(mocks.runStore.grantPermanentItem).toHaveBeenCalledWith('weapon');
    expect(mocks.playerStore.loadProfile).toHaveBeenCalled();
    expect(mocks.playerStore.equipItem).toHaveBeenCalledWith('char-1', 'weapon');
    expect(mocks.runStore.syncPartyStats).toHaveBeenCalled();

    wrapper.unmount();
    mocks.playerStore.permanentItems = [{ itemDefinitionKey: 'weapon' }];
    mocks.playerStore.profile = {
      characters: [{ id: 'char-1', items: [{ itemKey: 'weapon', isEquipped: true, slot: 'Weapon' }] }],
    };
    const equipped = mountPanel([weapon]);
    await flushPromises();
    await findCell(equipped, 'Lame').trigger('click');
    expect(equipped.find('.bp-btn').text()).toBe('Déséquiper');
    await equipped.find('.bp-btn').trigger('click');
    await flushPromises();
    expect(mocks.playerStore.unequipItem).toHaveBeenCalledWith('char-1', 'weapon');

    equipped.unmount();
    mocks.playerStore.profile = {
      characters: [{ id: 'char-1', items: [{ itemKey: 'other', isEquipped: true, slot: 'Weapon' }] }],
    };
    const full = mountPanel([weapon]);
    await flushPromises();
    await findCell(full, 'Lame').trigger('click');
    expect(full.text()).toContain('déjà occupé');
    expect(full.find('.bp-btn').attributes('disabled')).toBeDefined();
  });

  it('handles empty bags and catalog failures without surfacing an error', async () => {
    vi.mocked(itemsApi.listActive).mockRejectedValue('offline');
    const wrapper = mountPanel([], null);
    await flushPromises();

    expect(wrapper.text()).toContain('Ton sac est vide.');
    expect(wrapper.text()).toContain('0');
  });
});
