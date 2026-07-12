// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import LawsPopover from '../features/palace-laws/LawsPopover.vue';
import type { ActivePalaceLawDto, ActiveCurseDto, PalacePublicIndicatorDto, RunModifierDto } from '../features/runs/types/runTypes.ts';

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

const baseMod: RunModifierDto = {
  id: 'mod-1',
  type: 'AttackBonus',
  value: 5,
  duration: 'UntilRunEnds',
  sourceType: 'PalaceLaw',
  sourceKey: 'law-combat-1',
};

const baseIndicator: PalacePublicIndicatorDto = {
  key: 'palace.whispers',
  label: 'Murmures du Palais',
  description: 'Le Palais observe la traversée.',
  category: 'run',
  level: 'high',
  tone: 'mystery',
  source: 'run',
};

function mountPanel(
  laws?: ActivePalaceLawDto[] | null,
  curses?: ActiveCurseDto[] | null,
  modifiers?: RunModifierDto[] | null,
  roomClimate?: string | null,
  palaceIndicators?: PalacePublicIndicatorDto[] | null,
  lawDenialEnabled?: boolean,
  canUseLawDenial?: boolean,
) {
  return mount(LawsPopover, {
    props: {
      laws,
      curses,
      modifiers,
      palaceIndicators,
      roomClimate,
      showRoomClimate: roomClimate !== undefined,
      lawDenialEnabled,
      canUseLawDenial,
    },
  });
}

describe('LawsPopover', () => {
  it('renders without crashing', () => {
    expect(mountPanel().exists()).toBe(true);
  });

  it('shows the "Influences actives" title', () => {
    expect(mountPanel([baseLaw]).text()).toContain('Influences actives');
  });

  it('shows the empty state when no influences are provided', () => {
    expect(mountPanel([], []).text()).toContain('Aucune influence active');
  });

  it('shows the empty state when props are null', () => {
    expect(mountPanel(null, null).text()).toContain('Aucune influence active');
  });

  // ── Laws ──

  it('renders one entry per law', () => {
    const wrapper = mountPanel([baseLaw, { ...baseLaw, key: 'law-2', displayName: 'Loi du Givre' }]);
    expect(wrapper.findAll('.lp-law').length).toBe(2);
  });

  it('displays the law displayName', () => {
    expect(mountPanel([baseLaw]).text()).toContain('Loi du Sang');
  });

  it('falls back to law.key when displayName is empty', () => {
    const wrapper = mountPanel([{ ...baseLaw, displayName: '' }]);
    expect(wrapper.text()).toContain('law-combat-1');
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
    expect(mountPanel(null, [baseCurse]).text()).toContain('Malédictions');
  });

  it('displays the curse display name', () => {
    expect(mountPanel(null, [baseCurse]).text()).toContain('Silence du Palais');
  });

  it('falls back to curseDefinitionKey when displayName is absent', () => {
    const curse: ActiveCurseDto = { id: 'c2', curseDefinitionKey: 'curse.raw', displayName: null };
    expect(mountPanel(null, [curse]).text()).toContain('curse.raw');
  });

  it('shows the severity chip on a curse', () => {
    expect(mountPanel(null, [baseCurse]).text()).toContain('Major');
  });

  it('shows the "Consommée" badge when consumedAtUtc is set', () => {
    const consumed: ActiveCurseDto = { ...baseCurse, consumedAtUtc: '2026-06-01T00:00:00Z' };
    const wrapper = mountPanel(null, [consumed]);
    expect(wrapper.text()).toContain('Consommée');
    expect(wrapper.find('.lp-curse--consumed').exists()).toBe(true);
  });

  it('does not show "Consommée" badge when consumedAtUtc is null', () => {
    expect(mountPanel(null, [baseCurse]).text()).not.toContain('Consommée');
  });

  // ── Mixed ──

  it('shows both sections when laws and curses are present', () => {
    const wrapper = mountPanel([baseLaw], [baseCurse]);
    expect(wrapper.find('.lp-law').exists()).toBe(true);
    expect(wrapper.find('.lp-curse').exists()).toBe(true);
  });

  it('does not show a modifiers section when none are provided', () => {
    expect(mountPanel([baseLaw], [baseCurse]).find('.lp-mod').exists()).toBe(false);
  });

  // ── Emit ──

  // ── Modifiers ──

  it('shows the modifiers section when non-item modifiers are provided', () => {
    expect(mountPanel(null, null, [baseMod]).text()).toContain('Modificateurs');
  });

  it('renders one entry per visible modifier', () => {
    const mod2: RunModifierDto = { ...baseMod, id: 'mod-2', type: 'DefenseBonus' };
    const wrapper = mountPanel(null, null, [baseMod, mod2]);
    expect(wrapper.findAll('.lp-mod').length).toBe(2);
  });

  it('displays the modifier type', () => {
    expect(mountPanel(null, null, [baseMod]).text()).toContain('Attack Bonus');
  });

  it('displays the modifier value with + prefix for positive values', () => {
    expect(mountPanel(null, null, [baseMod]).text()).toContain('+5');
  });

  it('filters out RunItem-sourced modifiers', () => {
    const itemMod: RunModifierDto = { ...baseMod, id: 'mod-item', sourceType: 'RunItem', sourceKey: 'item.guard-shard' };
    const wrapper = mountPanel(null, null, [itemMod]);
    expect(wrapper.find('.lp-mod').exists()).toBe(false);
  });

  it('shows non-RunItem modifiers and hides RunItem modifiers when mixed', () => {
    const itemMod: RunModifierDto = { ...baseMod, id: 'mod-item', sourceType: 'RunItem' };
    const wrapper = mountPanel(null, null, [baseMod, itemMod]);
    expect(wrapper.findAll('.lp-mod').length).toBe(1);
  });

  it('shows empty state when only RunItem modifiers are provided', () => {
    const itemMod: RunModifierDto = { ...baseMod, sourceType: 'RunItem' };
    expect(mountPanel(null, null, [itemMod]).text()).toContain('Aucune influence active');
  });

  it('renders the Room climate panel when requested', () => {
    const wrapper = mountPanel(null, null, null, 'Rain');

    expect(wrapper.text()).toContain('Climat de Room');
    expect(wrapper.text()).toContain('Pluie');
  });

  it('renders the Room climate empty state when requested without active climate', () => {
    const wrapper = mountPanel(null, null, null, null);

    expect(wrapper.text()).toContain('Aucun climat actif dans cette Room.');
  });

  it('renders palace public indicators when provided', () => {
    const wrapper = mountPanel(null, null, null, undefined, [baseIndicator]);

    expect(wrapper.text()).toContain('Indicateurs du Palais');
    expect(wrapper.text()).toContain('Murmures du Palais');
    expect(wrapper.text()).toContain('Le Palais observe la traversée.');
  });

  it('renders palace indicator empty state when the run has no public indicators', () => {
    const wrapper = mountPanel(null, null, null, undefined, []);

    expect(wrapper.text()).toContain('Aucun indicateur public du Palais disponible.');
  });

  it('does not use activeModifiers as palace indicator source', () => {
    const wrapper = mountPanel(null, null, [baseMod], undefined, []);

    expect(wrapper.text()).toContain('Aucun indicateur public du Palais disponible.');
    expect(wrapper.findAll('.pic-root').length).toBe(0);
  });

  it('emits close when the close button is clicked', async () => {
    const wrapper = mountPanel([baseLaw]);
    await wrapper.find('.lp-close').trigger('click');
    expect(wrapper.emitted('close')).toBeDefined();
  });

  it('emits close when the footer button is clicked', async () => {
    const wrapper = mountPanel([baseLaw]);
    await wrapper.find('.lp-foot button').trigger('click');
    expect(wrapper.emitted('close')).toBeDefined();
  });

  // ── Law denial ("Déni permanent") ──

  it('does not show the revoke button when lawDenialEnabled is false', () => {
    const wrapper = mountPanel([baseLaw], null, null, undefined, undefined, false, false);
    expect(wrapper.find('.lp-law__revoke').exists()).toBe(false);
  });

  it('shows a disabled revoke button when enabled but on cooldown', () => {
    const wrapper = mountPanel([baseLaw], null, null, undefined, undefined, true, false);
    const button = wrapper.find('.lp-law__revoke');
    expect(button.exists()).toBe(true);
    expect((button.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('shows an enabled revoke button when usable', () => {
    const wrapper = mountPanel([baseLaw], null, null, undefined, undefined, true, true);
    const button = wrapper.find('.lp-law__revoke');
    expect(button.exists()).toBe(true);
    expect((button.element as HTMLButtonElement).disabled).toBe(false);
  });

  it('emits revokeLaw with the law key when the revoke button is clicked', async () => {
    const wrapper = mountPanel([baseLaw], null, null, undefined, undefined, true, true);
    await wrapper.find('.lp-law__revoke').trigger('click');
    expect(wrapper.emitted('revokeLaw')).toEqual([[baseLaw.key]]);
  });

  it('renders one revoke button per law when there are several active laws', () => {
    const wrapper = mountPanel(
      [baseLaw, { ...baseLaw, key: 'law-2', displayName: 'Loi du Givre' }],
      null, null, undefined, undefined, true, true,
    );
    expect(wrapper.findAll('.lp-law__revoke').length).toBe(2);
  });
});
