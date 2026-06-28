// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import CombatOutcomePanel from './CombatOutcomePanel.vue';

function mountPanel(isVictory: boolean, isLoading: boolean) {
  return mount(CombatOutcomePanel, { props: { isVictory, isLoading } });
}

describe('CombatOutcomePanel', () => {
  it('renders without crashing', () => {
    const wrapper = mountPanel(true, false);
    expect(wrapper.exists()).toBe(true);
  });

  it('applies victory class when isVictory is true', () => {
    const wrapper = mountPanel(true, false);
    expect(wrapper.find('.cop-root').classes()).toContain('cop-root--victory');
  });

  it('applies defeat class when isVictory is false', () => {
    const wrapper = mountPanel(false, false);
    expect(wrapper.find('.cop-root').classes()).toContain('cop-root--defeat');
  });

  it('displays VICTOIRE title for victory', () => {
    const wrapper = mountPanel(true, false);
    expect(wrapper.find('.cop-title').text()).toBe('VICTOIRE');
  });

  it('displays DÉFAITE title for defeat', () => {
    const wrapper = mountPanel(false, false);
    expect(wrapper.find('.cop-title').text()).toBe('DÉFAITE');
  });

  it('shows victory description', () => {
    const wrapper = mountPanel(true, false);
    expect(wrapper.find('.cop-desc').text()).toContain('Les manifestations se sont tues');
  });

  it('shows defeat description', () => {
    const wrapper = mountPanel(false, false);
    expect(wrapper.find('.cop-desc').text()).toContain('Les voix se sont éteintes');
  });

  it('shows continue button for victory', () => {
    const wrapper = mountPanel(true, false);
    expect(wrapper.find('.cop-btn--victory').exists()).toBe(true);
    expect(wrapper.find('.cop-btn--victory').text()).toContain('Continuer');
  });

  it('shows leave button for defeat', () => {
    const wrapper = mountPanel(false, false);
    expect(wrapper.find('.cop-btn--defeat').exists()).toBe(true);
    expect(wrapper.find('.cop-btn--defeat').text()).toContain('Quitter');
  });

  it('emits continue when victory button is clicked', async () => {
    const wrapper = mountPanel(true, false);
    await wrapper.find('.cop-btn--victory').trigger('click');
    expect(wrapper.emitted('continue')).toBeDefined();
    expect(wrapper.emitted('continue')?.length).toBe(1);
  });

  it('emits leaveRun when defeat button is clicked', async () => {
    const wrapper = mountPanel(false, false);
    await wrapper.find('.cop-btn--defeat').trigger('click');
    expect(wrapper.emitted('leaveRun')).toBeDefined();
    expect(wrapper.emitted('leaveRun')?.length).toBe(1);
  });

  it('disables button when isLoading is true (victory)', () => {
    const wrapper = mountPanel(true, true);
    const btn = wrapper.find('.cop-btn--victory');
    expect((btn.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('disables button when isLoading is true (defeat)', () => {
    const wrapper = mountPanel(false, true);
    const btn = wrapper.find('.cop-btn--defeat');
    expect((btn.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('shows spinner when isLoading is true (victory)', () => {
    const wrapper = mountPanel(true, true);
    expect(wrapper.find('.cop-spinner').exists()).toBe(true);
  });

  it('shows spinner when isLoading is true (defeat)', () => {
    const wrapper = mountPanel(false, true);
    expect(wrapper.find('.cop-spinner').exists()).toBe(true);
  });

  it('hides spinner when not loading (victory)', () => {
    const wrapper = mountPanel(true, false);
    expect(wrapper.find('.cop-spinner').exists()).toBe(false);
  });

  it('hides spinner when not loading (defeat)', () => {
    const wrapper = mountPanel(false, false);
    expect(wrapper.find('.cop-spinner').exists()).toBe(false);
  });

  it('renders decorative particles', () => {
    const wrapper = mountPanel(true, false);
    expect(wrapper.find('.cop-particles').exists()).toBe(true);
  });

  it('renders corner ornaments', () => {
    const wrapper = mountPanel(true, false);
    expect(wrapper.findAll('.cop-corner').length).toBe(4);
  });

  it('renders grain overlay', () => {
    const wrapper = mountPanel(true, false);
    expect(wrapper.find('.cop-grain').exists()).toBe(true);
  });

  it('renders backdrop', () => {
    const wrapper = mountPanel(true, false);
    expect(wrapper.find('.cop-backdrop').exists()).toBe(true);
  });

  it('shows combat terminé kicker', () => {
    const wrapper = mountPanel(true, false);
    expect(wrapper.find('.cop-kicker').text()).toContain('Combat terminé');
  });
});
