// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import SkillDetailModal from './SkillDetailModal.vue';
import type { SkillDefinitionView } from '../../features/party/types/skillTypes';

function baseSkill(overrides: Partial<SkillDefinitionView> = {}): SkillDefinitionView {
  return {
    key: 'skill.strike',
    displayName: 'Frappe',
    description: 'Un coup net.',
    skillType: 'Active',
    targetingType: 'SingleEnemy',
    effectType: 'Damage',
    manaCost: 0,
    chargeCost: 1,
    basePower: 12,
    category: 'Physical',
    basePowerIsPercentOfMaxVitality: false,
    effects: [],
    acquisitionHints: [],
    ...overrides,
  };
}

function mountModal(skill: SkillDefinitionView | null) {
  return mount(SkillDetailModal, {
    props: { skill },
    global: {
      stubs: {
        Teleport: { template: '<slot />' },
        Transition: { template: '<slot />' },
      },
    },
  });
}

describe('SkillDetailModal', () => {
  it('renders nothing when skill is null', () => {
    const wrapper = mountModal(null);
    expect(wrapper.find('.skill-modal-backdrop').exists()).toBe(false);
  });

  it('shows the skill name and description', () => {
    const wrapper = mountModal(baseSkill());
    expect(wrapper.text()).toContain('Frappe');
    expect(wrapper.text()).toContain('Un coup net.');
  });

  it('shows the ultimate badge only for ultimate skills', () => {
    const wrapper = mountModal(baseSkill({ isUltimate: true }));
    expect(wrapper.text()).toContain('Ultime');
  });

  it('does not show the ultimate badge for regular skills', () => {
    const wrapper = mountModal(baseSkill({ isUltimate: false }));
    expect(wrapper.find('.skill-modal-chip--ultimate').exists()).toBe(false);
  });

  it('lists formatted effects', () => {
    const wrapper = mountModal(baseSkill({
      effects: [{
        kind: 'StatModifier',
        statusKey: null,
        magnitude: -10,
        durationTicks: 3,
        tickInterval: 1,
        stat: 'Defense',
        magnitudeIsPercentOfMax: false,
        magnitudeIsPercentOfBaseStat: false,
        appliesToActor: false,
        isPermanent: false,
      }],
    }));
    expect(wrapper.text()).toContain('Défense -10');
  });

  it('emits close when the close button is clicked', async () => {
    const wrapper = mountModal(baseSkill());
    await wrapper.find('.skill-modal__close').trigger('click');
    expect(wrapper.emitted('close')).toHaveLength(1);
  });

  it('emits close when the backdrop is clicked', async () => {
    const wrapper = mountModal(baseSkill());
    await wrapper.find('.skill-modal-backdrop').trigger('click');
    expect(wrapper.emitted('close')).toHaveLength(1);
  });

  it('does not emit close when the panel itself is clicked', async () => {
    const wrapper = mountModal(baseSkill());
    await wrapper.find('.skill-modal').trigger('click');
    expect(wrapper.emitted('close')).toBeUndefined();
  });
});
