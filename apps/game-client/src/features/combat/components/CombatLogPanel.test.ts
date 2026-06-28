// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import CombatLogPanel from './CombatLogPanel.vue';
import type { CombatLogEntryDto } from '../types/combatContracts';

function makeEntry(type: CombatLogEntryDto['type'], message: string): CombatLogEntryDto {
  return {
    occurredAtUtc: new Date().toISOString(),
    type,
    message,
    actorId: 'ally-1',
    skillKey: null,
    targetIds: ['enemy-1'],
  };
}

function mountPanel(entries: CombatLogEntryDto[]) {
  return mount(CombatLogPanel, { props: { entries } });
}

describe('CombatLogPanel', () => {
  it('renders without crashing', () => {
    const wrapper = mountPanel([]);
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the panel title', () => {
    const wrapper = mountPanel([]);
    expect(wrapper.text()).toContain('Journal de combat');
  });

  it('shows the entry count', () => {
    const entries = [
      makeEntry('SkillUsed', 'Action 1'),
      makeEntry('DamageApplied', 'Damage 1'),
    ];
    const wrapper = mountPanel(entries);
    expect(wrapper.text()).toContain('2');
  });

  it('shows zero count when no entries', () => {
    const wrapper = mountPanel([]);
    expect(wrapper.text()).toContain('0');
  });

  it('shows empty state message when no entries', () => {
    const wrapper = mountPanel([]);
    expect(wrapper.text()).toContain('En attente de la première action…');
  });

  it('does not show empty state when entries exist', () => {
    const wrapper = mountPanel([makeEntry('SkillUsed', 'Action')]);
    expect(wrapper.find('.combat-log__empty').exists()).toBe(false);
  });

  it('renders entries when panel is open', async () => {
    const entries = [makeEntry('SkillUsed', 'Hero attacks')];
    const wrapper = mountPanel(entries);
    await wrapper.find('.combat-log__tab').trigger('click');
    expect(wrapper.find('.combat-log__entry').exists()).toBe(true);
  });

  it('toggles panel open on tab click', async () => {
    const wrapper = mountPanel([]);
    expect(wrapper.find('.combat-log').classes()).not.toContain('combat-log--open');
    await wrapper.find('.combat-log__tab').trigger('click');
    expect(wrapper.find('.combat-log').classes()).toContain('combat-log--open');
  });

  it('toggles panel closed on second tab click', async () => {
    const wrapper = mountPanel([]);
    await wrapper.find('.combat-log__tab').trigger('click');
    await wrapper.find('.combat-log__tab').trigger('click');
    expect(wrapper.find('.combat-log').classes()).not.toContain('combat-log--open');
  });

  it('shows chevron down when open', async () => {
    const wrapper = mountPanel([]);
    await wrapper.find('.combat-log__tab').trigger('click');
    expect(wrapper.find('.combat-log__chevron').text()).toBe('▾');
  });

  it('shows chevron up when closed', () => {
    const wrapper = mountPanel([]);
    expect(wrapper.find('.combat-log__chevron').text()).toBe('▴');
  });

  it('applies type-based CSS class to entries', async () => {
    const entries = [makeEntry('DamageApplied', 'Damage dealt')];
    const wrapper = mountPanel(entries);
    await wrapper.find('.combat-log__tab').trigger('click');
    expect(wrapper.find('.combat-log__entry').classes()).toContain('combat-log__entry--damageapplied');
  });

  it('displays entry type label', async () => {
    const entries = [makeEntry('SkillUsed', 'Action')];
    const wrapper = mountPanel(entries);
    await wrapper.find('.combat-log__tab').trigger('click');
    expect(wrapper.find('.combat-log__type').text()).toBe('SkillUsed');
  });

  it('displays entry message', async () => {
    const entries = [makeEntry('DamageApplied', '15 damage')];
    const wrapper = mountPanel(entries);
    await wrapper.find('.combat-log__tab').trigger('click');
    expect(wrapper.find('.combat-log__msg').text()).toBe('15 damage');
  });

  it('renders multiple entries', async () => {
    const entries = [
      makeEntry('SkillUsed', 'Action 1'),
      makeEntry('DamageApplied', 'Damage 1'),
      makeEntry('HealApplied', 'Heal 1'),
    ];
    const wrapper = mountPanel(entries);
    await wrapper.find('.combat-log__tab').trigger('click');
    expect(wrapper.findAll('.combat-log__entry').length).toBe(3);
  });
});
