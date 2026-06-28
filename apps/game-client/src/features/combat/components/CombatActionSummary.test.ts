// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import CombatActionSummary from './CombatActionSummary.vue';
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

function mountSummary(entries: CombatLogEntryDto[]) {
  return mount(CombatActionSummary, { props: { entries } });
}

describe('CombatActionSummary', () => {
  it('renders without crashing with entries', () => {
    const wrapper = mountSummary([makeEntry('SkillUsed', 'Hero uses Frappe')]);
    expect(wrapper.exists()).toBe(true);
  });

  it('does not render the container when entries are empty', () => {
    const wrapper = mountSummary([]);
    expect(wrapper.find('.cas').exists()).toBe(false);
  });

  it('renders one entry per log entry', () => {
    const entries = [
      makeEntry('SkillUsed', 'Hero uses Frappe'),
      makeEntry('DamageApplied', 'Enemy takes 10 damage'),
    ];
    const wrapper = mountSummary(entries);
    expect(wrapper.findAll('.cas__entry').length).toBe(2);
  });

  it('displays the message for each entry', () => {
    const wrapper = mountSummary([makeEntry('SkillUsed', 'Hero uses Frappe')]);
    expect(wrapper.find('.cas__msg').text()).toBe('Hero uses Frappe');
  });

  it('applies action class for SkillUsed entries', () => {
    const wrapper = mountSummary([makeEntry('SkillUsed', 'Action')]);
    expect(wrapper.find('.cas__entry').classes()).toContain('cas__entry--action');
  });

  it('applies action class for ItemUsed entries', () => {
    const wrapper = mountSummary([makeEntry('ItemUsed', 'Item used')]);
    expect(wrapper.find('.cas__entry').classes()).toContain('cas__entry--action');
  });

  it('applies damage class for DamageApplied entries', () => {
    const wrapper = mountSummary([makeEntry('DamageApplied', '10 damage')]);
    expect(wrapper.find('.cas__entry').classes()).toContain('cas__entry--damage');
  });

  it('applies guard class for GuardGained entries', () => {
    const wrapper = mountSummary([makeEntry('GuardGained', 'Guard gained')]);
    expect(wrapper.find('.cas__entry').classes()).toContain('cas__entry--guard');
  });

  it('applies heal class for HealApplied entries', () => {
    const wrapper = mountSummary([makeEntry('HealApplied', 'Healed')]);
    expect(wrapper.find('.cas__entry').classes()).toContain('cas__entry--heal');
  });

  it('applies defeated class for TargetDefeated entries', () => {
    const wrapper = mountSummary([makeEntry('TargetDefeated', 'Defeated')]);
    expect(wrapper.find('.cas__entry').classes()).toContain('cas__entry--defeated');
  });

  it('applies dim class for TurnAdvanced entries', () => {
    const wrapper = mountSummary([makeEntry('TurnAdvanced', 'Turn advanced')]);
    expect(wrapper.find('.cas__entry').classes()).toContain('cas__entry--dim');
  });

  it('applies dim class for EnemyTurnResolved entries', () => {
    const wrapper = mountSummary([makeEntry('EnemyTurnResolved', 'Enemy turn')]);
    expect(wrapper.find('.cas__entry').classes()).toContain('cas__entry--dim');
  });

  it('applies completed class for CombatCompleted entries', () => {
    const wrapper = mountSummary([makeEntry('CombatCompleted', 'Combat completed')]);
    expect(wrapper.find('.cas__entry').classes()).toContain('cas__entry--completed');
  });

  it('applies failed class for CombatFailed entries', () => {
    const wrapper = mountSummary([makeEntry('CombatFailed', 'Combat failed')]);
    expect(wrapper.find('.cas__entry').classes()).toContain('cas__entry--failed');
  });

  it('shows KO badge for TargetDefeated entries', () => {
    const wrapper = mountSummary([makeEntry('TargetDefeated', 'Defeated')]);
    expect(wrapper.find('.cas__badge').text()).toBe('KO');
  });

  it('does not show KO badge for non-TargetDefeated entries', () => {
    const wrapper = mountSummary([makeEntry('SkillUsed', 'Action')]);
    expect(wrapper.find('.cas__badge').exists()).toBe(false);
  });

  it('shows arrow for SkillUsed entries', () => {
    const wrapper = mountSummary([makeEntry('SkillUsed', 'Action')]);
    expect(wrapper.find('.cas__arrow').exists()).toBe(true);
  });

  it('shows arrow for ItemUsed entries', () => {
    const wrapper = mountSummary([makeEntry('ItemUsed', 'Item used')]);
    expect(wrapper.find('.cas__arrow').exists()).toBe(true);
  });

  it('does not show arrow for DamageApplied entries', () => {
    const wrapper = mountSummary([makeEntry('DamageApplied', 'Damage')]);
    expect(wrapper.find('.cas__arrow').exists()).toBe(false);
  });
});
