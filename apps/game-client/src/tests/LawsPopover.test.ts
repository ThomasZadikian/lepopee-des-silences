// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import LawsPopover from '../features/palace-laws/LawsPopover.vue';
import type { ActivePalaceLawDto } from '../features/runs/types/runTypes.ts';

const baseLaw: ActivePalaceLawDto = {
  key: 'law-combat-1',
  version: '1.0',
  displayName: 'Loi du Sang',
  description: 'Chaque combat commence avec de la garde.',
  rarity: 'Commun',
  polarity: 'Neutre',
  domains: ['Combat'],
};

function mountPanel(
  laws?: ActivePalaceLawDto[] | null,
  roomClimate?: string | null,
  lawDenialEnabled?: boolean,
  canUseLawDenial?: boolean,
) {
  return mount(LawsPopover, {
    props: {
      laws,
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

  it('shows the "Lois du Palais" kicker', () => {
    expect(mountPanel([baseLaw]).text()).toContain('Lois du Palais');
  });

  it('shows the empty state when no laws are provided', () => {
    expect(mountPanel([]).text()).toContain('Aucune loi active.');
  });

  it('shows the empty state when props are null', () => {
    expect(mountPanel(null).text()).toContain('Aucune loi active.');
  });

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

  it('renders the Room climate panel when requested', () => {
    const wrapper = mountPanel(null, 'Rain');

    expect(wrapper.text()).toContain('Climat de Room');
    expect(wrapper.text()).toContain('Pluie');
  });

  it('renders the Room climate empty state when requested without active climate', () => {
    const wrapper = mountPanel(null, null);

    expect(wrapper.text()).toContain('Aucun climat actif dans cette Room.');
  });

  it('emits close when the close button is clicked', async () => {
    const wrapper = mountPanel([baseLaw]);
    await wrapper.find('.lp-close').trigger('click');
    expect(wrapper.emitted('close')).toBeDefined();
  });

  // ── Law denial ("Déni permanent") ──

  it('does not show the revoke button when lawDenialEnabled is false', () => {
    const wrapper = mountPanel([baseLaw], undefined, false, false);
    expect(wrapper.find('.lp-revoke').exists()).toBe(false);
  });

  it('shows a disabled revoke button when enabled but on cooldown', () => {
    const wrapper = mountPanel([baseLaw], undefined, true, false);
    const button = wrapper.find('.lp-revoke');
    expect(button.exists()).toBe(true);
    expect((button.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('shows an enabled revoke button when usable', () => {
    const wrapper = mountPanel([baseLaw], undefined, true, true);
    const button = wrapper.find('.lp-revoke');
    expect(button.exists()).toBe(true);
    expect((button.element as HTMLButtonElement).disabled).toBe(false);
  });

  it('emits revokeLaw with the law key when the revoke button is clicked', async () => {
    const wrapper = mountPanel([baseLaw], undefined, true, true);
    await wrapper.find('.lp-revoke').trigger('click');
    expect(wrapper.emitted('revokeLaw')).toEqual([[baseLaw.key]]);
  });

  it('renders one revoke button per law when there are several active laws', () => {
    const wrapper = mountPanel(
      [baseLaw, { ...baseLaw, key: 'law-2', displayName: 'Loi du Givre' }],
      undefined, true, true,
    );
    expect(wrapper.findAll('.lp-revoke').length).toBe(2);
  });
});
