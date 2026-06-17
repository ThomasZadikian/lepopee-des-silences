// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import LawsPopover from './LawsPopover.vue';
import type { ActivePalaceLawDto, ActiveCurseDto, RunModifierDto } from '../runs/types/runTypes';

const baseLaw: ActivePalaceLawDto = {
  key: 'law-combat-1',
  version: '1.0',
  displayName: 'Loi du Sang',
  description: 'Chaque combat commence avec de la garde.',
  domain: 'Combat',
};

const baseCurse: ActiveCurseDto = {
  id: 'curse-1',
  curseDefinitionKey: 'curse.silence',
  displayName: 'Silence du Palais',
  description: 'Tes alliés ne peuvent utiliser aucune compétence.',
  severity: 'Major',
  duration: 'UntilRunEnds',
};

const baseModifier: RunModifierDto = {
  id: 'mod-1',
  type: 'ModifyAttackPower',
  value: 5,
  duration: 'UntilRunEnds',
  sourceType: 'PalaceLaw',
  sourceKey: 'law-combat-1',
};

function mountPanel(
  laws?: ActivePalaceLawDto[] | null,
  curses?: ActiveCurseDto[] | null,
  modifiers?: RunModifierDto[] | null,
) {
  return mount(LawsPopover, { props: { laws, curses, modifiers } });
}

describe('LawsPopover', () => {
  it('renders without crashing', () => {
    expect(mountPanel().exists()).toBe(true);
  });

  it('shows the "Influences actives" title', () => {
    expect(mountPanel([baseLaw]).text()).toContain('Influences actives');
  });

  it('shows the empty state when no influences are provided', () => {
    const wrapper = mountPanel([], [], []);
    expect(wrapper.text()).toContain('Aucune influence active');
  });

  it('shows the empty state when props are null', () => {
    const wrapper = mountPanel(null, null, null);
    expect(wrapper.text()).toContain('Aucune influence active');
  });

  // ── Laws ──

  it('renders one entry per law', () => {
    const wrapper = mountPanel([baseLaw, { ...baseLaw, key: 'law-2', displayName: 'Loi du Givre' }]);
    expect(wrapper.findAll('.lp-law').length).toBe(2);
  });

  it('displays the law name', () => {
    expect(mountPanel([baseLaw]).text()).toContain('Loi du Sang');
  });

  it('displays the law description', () => {
    expect(mountPanel([baseLaw]).text()).toContain('Chaque combat commence avec de la garde.');
  });

  it('displays the law domain chip', () => {
    expect(mountPanel([baseLaw]).text()).toContain('Combat');
  });

  it('displays the law version', () => {
    expect(mountPanel([baseLaw]).text()).toContain('1.0');
  });

  it('shows "Lois du Palais" section title when laws are present', () => {
    expect(mountPanel([baseLaw]).text()).toContain('Lois du Palais');
  });

  // ── Curses ──

  it('shows the curses section when curses are provided', () => {
    const wrapper = mountPanel(null, [baseCurse]);
    expect(wrapper.text()).toContain('Malédictions');
  });

  it('displays the curse display name', () => {
    expect(mountPanel(null, [baseCurse]).text()).toContain('Silence du Palais');
  });

  it('falls back to curseDefinitionKey when displayName is absent', () => {
    const curse: ActiveCurseDto = { id: 'c2', curseDefinitionKey: 'curse.raw', displayName: null };
    expect(mountPanel(null, [curse]).text()).toContain('curse.raw');
  });

  it('shows the severity chip on a curse', () => {
    const wrapper = mountPanel(null, [baseCurse]);
    expect(wrapper.text()).toContain('Major');
  });

  it('shows the "Consommée" badge when consumedAtUtc is set', () => {
    const consumed: ActiveCurseDto = { ...baseCurse, consumedAtUtc: '2026-06-01T00:00:00Z' };
    const wrapper = mountPanel(null, [consumed]);
    expect(wrapper.text()).toContain('Consommée');
    expect(wrapper.find('.lp-curse--consumed').exists()).toBe(true);
  });

  it('does not show "Consommée" badge when consumedAtUtc is null', () => {
    const wrapper = mountPanel(null, [baseCurse]);
    expect(wrapper.text()).not.toContain('Consommée');
  });

  // ── Modifiers ──

  it('shows the modifiers section when modifiers are provided', () => {
    const wrapper = mountPanel(null, null, [baseModifier]);
    expect(wrapper.text()).toContain('Modificateurs actifs');
  });

  it('displays the modifier value with sign for positive', () => {
    const wrapper = mountPanel(null, null, [baseModifier]);
    expect(wrapper.find('.lp-mod__value--pos').exists()).toBe(true);
    expect(wrapper.find('.lp-mod__value--pos').text()).toContain('+5');
  });

  it('displays the modifier value for negative values', () => {
    const neg: RunModifierDto = { ...baseModifier, value: -3 };
    const wrapper = mountPanel(null, null, [neg]);
    expect(wrapper.find('.lp-mod__value--neg').exists()).toBe(true);
    expect(wrapper.find('.lp-mod__value--neg').text()).toContain('-3');
  });

  it('shows the human-readable modifier type label', () => {
    expect(mountPanel(null, null, [baseModifier]).text()).toContain('Attaque');
  });

  it('shows the duration label for modifiers', () => {
    expect(mountPanel(null, null, [baseModifier]).text()).toContain('run entière');
  });

  it('shows the source label for modifiers', () => {
    expect(mountPanel(null, null, [baseModifier]).text()).toContain('Loi du Palais');
  });

  // ── Mixed ──

  it('shows all three sections when all data is present', () => {
    const wrapper = mountPanel([baseLaw], [baseCurse], [baseModifier]);
    expect(wrapper.find('.lp-law').exists()).toBe(true);
    expect(wrapper.find('.lp-curse').exists()).toBe(true);
    expect(wrapper.find('.lp-mod').exists()).toBe(true);
  });

  // ── Emit ──

  it('emits close when the close button is clicked', async () => {
    const wrapper = mountPanel([baseLaw]);
    await wrapper.find('.lp-close').trigger('click');
    expect(wrapper.emitted('close')).toBeDefined();
  });

  it('emits close when the footer button is clicked', async () => {
    const wrapper = mountPanel([baseLaw]);
    const footer = wrapper.find('.lp-foot button');
    await footer.trigger('click');
    expect(wrapper.emitted('close')).toBeDefined();
  });
});
