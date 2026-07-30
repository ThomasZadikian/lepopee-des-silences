// @vitest-environment jsdom
import { beforeEach, describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import PartyDrawer from './PartyDrawer.vue';
import type { RunPartyMemberDto, RunModifierDto, ActivePalaceLawDto, ActiveCurseDto, RunItemDto } from '../types/runTypes';

beforeEach(() => {
  setActivePinia(createPinia());
});

function mountDrawer(
  allies: RunPartyMemberDto[] | null = null,
  modifiers: RunModifierDto[] | null = null,
  laws: ActivePalaceLawDto[] | null = null,
  curses: ActiveCurseDto[] | null = null,
  items: RunItemDto[] | null = null,
  caliceInfiniEnabled = false,
  canUseCaliceInfini = false,
) {
  return mount(PartyDrawer, {
    props: { allies, modifiers, laws, curses, items, caliceInfiniEnabled, canUseCaliceInfini },
    global: {
      stubs: {
        Transition: { template: '<slot />' },
      },
    },
  });
}

describe('PartyDrawer', () => {
  const baseAlly: RunPartyMemberDto = {
    id: 'ally-1',
    displayName: 'Héros',
    maxVitality: 100,
    currentVitality: 75,
    guard: 10,
    mana: 5,
    charge: 2,
    isActive: true,
    isDefeated: false,
    skills: [{ key: 'skill.strike', displayName: 'Frappe', skillType: 'Damage', targetingMode: 'Single' }],
  };

  const baseModifier: RunModifierDto = {
    id: 'mod-1',
    type: 'AddStartingGuard',
    value: 5,
    duration: 'UntilRunEnds',
    sourceType: 'Item',
  };

  const baseLaw: ActivePalaceLawDto = {
    key: 'law-aegis-v1',
    displayName: 'Loi d\'Aegis',
    domains: ['Combat'],
    version: 'v1',
    description: 'Protège les alliés.',
    rarity: 'Commun',
    polarity: 'Neutre',
  };

  const baseCurse: ActiveCurseDto = {
    id: 'curse-1',
    curseDefinitionKey: 'curse.old-wound',
    displayName: 'Blessure ancienne',
    severity: 'Moderate',
    description: 'Une vieille blessure qui ne guérit pas.',
  };

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
    const wrapper = mountDrawer();
    expect(wrapper.exists()).toBe(true);
  });

  it('emits close when close button is clicked', async () => {
    const wrapper = mountDrawer();
    await wrapper.find('.party-drawer__close').trigger('click');
    expect(wrapper.emitted('close')).toHaveLength(1);
  });

  it('displays allies section with data', () => {
    const wrapper = mountDrawer([baseAlly]);
    expect(wrapper.text()).toContain('Alliés');
    expect(wrapper.text()).toContain('Héros');
  });

  it('shows vitality stats for allies', () => {
    const wrapper = mountDrawer([baseAlly]);
    expect(wrapper.text()).toContain('75');
    expect(wrapper.text()).toContain('100');
  });

  it('shows guard stat when guard > 0', () => {
    const wrapper = mountDrawer([baseAlly]);
    expect(wrapper.text()).toContain('GARDE');
    expect(wrapper.text()).toContain('10');
  });

  it('shows Mana stat when mana > 0', () => {
    const wrapper = mountDrawer([baseAlly]);
    expect(wrapper.text()).toContain('Mana');
    expect(wrapper.text()).toContain('5');
  });

  it('shows Mana stat even when mana is 0 (a core resource, not a transient buff)', () => {
    const wrapper = mountDrawer([{ ...baseAlly, mana: 0 }]);
    expect(wrapper.text()).toContain('Mana');
  });

  it('shows charge stat when charge > 0', () => {
    const wrapper = mountDrawer([baseAlly]);
    expect(wrapper.text()).toContain('CHARGE');
    expect(wrapper.text()).toContain('2');
  });

  it('shows KO chip for defeated allies', () => {
    const ally = { ...baseAlly, isDefeated: true };
    const wrapper = mountDrawer([ally]);
    expect(wrapper.text()).toContain('KO');
  });

  it('applies defeated class for defeated allies', () => {
    const ally = { ...baseAlly, isDefeated: true };
    const wrapper = mountDrawer([ally]);
    expect(wrapper.find('.party-card--defeated').exists()).toBe(true);
  });

  it('shows empty state when no allies', () => {
    const wrapper = mountDrawer([]);
    expect(wrapper.text()).toContain('Données disponibles en combat uniquement.');
  });

  it('displays modifiers section', () => {
    const wrapper = mountDrawer(null, [baseModifier]);
    expect(wrapper.text()).toContain('Modificateurs actifs');
    expect(wrapper.text()).toContain('Garde initiale');
    expect(wrapper.text()).toContain('+5');
  });

  it('displays curses section', () => {
    const wrapper = mountDrawer(null, null, null, [baseCurse]);
    expect(wrapper.text()).toContain('Malédictions');
    expect(wrapper.text()).toContain('Blessure ancienne');
  });

  it('displays laws section', () => {
    const wrapper = mountDrawer(null, null, [baseLaw]);
    expect(wrapper.text()).toContain('Lois du Palais');
    expect(wrapper.text()).toContain('Loi d\'Aegis');
  });

  it('displays items section', () => {
    const wrapper = mountDrawer(null, null, null, null, [baseItem]);
    expect(wrapper.text()).toContain('Objets de run');
    expect(wrapper.text()).toContain('Potion de soin');
  });

  it('shows quantity for items with quantity > 1', () => {
    const item = { ...baseItem, quantity: 3 };
    const wrapper = mountDrawer(null, null, null, null, [item]);
    expect(wrapper.text()).toContain('×3');
  });

  it('shows rarity tone for epic items', () => {
    const item = { ...baseItem, rarity: 'Epic' };
    const wrapper = mountDrawer(null, null, null, null, [item]);
    const chip = wrapper.find('.party-drawer__item .es-chip');
    expect(chip.classes()).toContain('es-chip--gold');
  });

  it('shows empty state when all sections are empty', () => {
    const wrapper = mountDrawer([], [], [], [], []);
    expect(wrapper.text()).toContain('Aucune donnée d\'équipe disponible.');
  });

  it('calculates vitality percentage correctly', () => {
    const ally = { ...baseAlly, currentVitality: 25, maxVitality: 100 };
    const wrapper = mountDrawer([ally]);
    const bar = wrapper.find('.party-card__bar-fill');
    expect(bar.attributes('style')).toContain('width: 25%');
  });

  it('handles zero maxVitality gracefully', () => {
    const ally = { ...baseAlly, maxVitality: 0, currentVitality: 0 };
    const wrapper = mountDrawer([ally]);
    expect(wrapper.exists()).toBe(true);
  });

  it('shows skills when available', () => {
    const wrapper = mountDrawer([baseAlly]);
    expect(wrapper.text()).toContain('Frappe');
  });

  it('hides skills section when no skills', () => {
    const ally = { ...baseAlly, skills: [] };
    const wrapper = mountDrawer([ally]);
    expect(wrapper.find('.party-card__skills').exists()).toBe(false);
  });

  it('formats modifier duration correctly', () => {
    const mod = { ...baseModifier, duration: 'Permanent' };
    const wrapper = mountDrawer(null, [mod]);
    expect(wrapper.text()).toContain('permanent');
  });

  it('formats modifier source correctly', () => {
    const mod = { ...baseModifier, sourceType: 'PalaceLaw' };
    const wrapper = mountDrawer(null, [mod]);
    expect(wrapper.text()).toContain('Loi du Palais');
  });

  it('handles null props gracefully', () => {
    const wrapper = mountDrawer(null, null, null, null, null);
    expect(wrapper.exists()).toBe(true);
  });

  it('does not show the Calice infini button when the player does not own it', () => {
    const wrapper = mountDrawer(null, null, null, null, null, false, false);
    expect(wrapper.text()).not.toContain('Calice infini');
  });

  it('shows an enabled Calice infini button when usable', () => {
    const wrapper = mountDrawer(null, null, null, null, null, true, true);
    const button = wrapper.findAll('button').find((b) => b.text().includes('Calice infini'));
    expect(button).toBeTruthy();
    expect(button!.attributes('disabled')).toBeUndefined();
  });

  it('disables the Calice infini button while on cooldown', () => {
    const wrapper = mountDrawer(null, null, null, null, null, true, false);
    const button = wrapper.findAll('button').find((b) => b.text().includes('Calice infini'));
    expect(button).toBeTruthy();
    expect(button!.attributes('disabled')).toBeDefined();
  });

});
