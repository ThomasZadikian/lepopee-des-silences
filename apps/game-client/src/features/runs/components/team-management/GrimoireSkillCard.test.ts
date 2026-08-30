// @vitest-environment jsdom
import { beforeEach, describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';

import GrimoireSkillCard from './GrimoireSkillCard.vue';
import type { SkillDefinitionView } from '../../../party/types/skillTypes';
import { useEmotionalRegisterCatalog } from '../../../emotional-registers/store';

beforeEach(() => {
  useEmotionalRegisterCatalog().install('test-1', [
    {
      code: 'neutral', displayName: 'Neutral', glyph: '·', color: 'gray',
      incomingAffinities: [
        { incomingRegister: 'neutral', outcome: 'Neutral', multiplier: 1 },
        { incomingRegister: 'effroi', outcome: 'Neutral', multiplier: 1 },
      ],
    },
    {
      code: 'effroi', displayName: 'Effroi', glyph: '✶', color: 'red',
      incomingAffinities: [
        { incomingRegister: 'neutral', outcome: 'Neutral', multiplier: 1 },
        { incomingRegister: 'effroi', outcome: 'Neutral', multiplier: 1 },
      ],
    },
  ]);
});

function skill(overrides: Partial<SkillDefinitionView> = {}): SkillDefinitionView {
  return {
    key: 'skill.a', displayName: 'Frappe', description: 'Un coup.', skillType: 'Damage',
    targetingType: 'SingleEnemy', effectType: 'Damage', manaCost: 0, chargeCost: 0, basePower: 10,
    category: 'Physical', basePowerIsPercentOfMaxVitality: false, effects: [], acquisitionHints: [],
    emotionalRegister: 'neutral',
    compatibleCharacterDefinitionKeys: ['character.player.self'],
    ...overrides,
  };
}

describe('GrimoireSkillCard', () => {
  it('shows an equip toggle for a known skill', () => {
    const wrapper = mount(GrimoireSkillCard, {
      props: { skill: skill(), isKnown: true, isEquipped: false, disabled: false },
    });

    expect(wrapper.find('.grimoire-toggle').text()).toBe('Équiper');
    expect(wrapper.find('.grimoire-lock-hint').exists()).toBe(false);
  });

  it('shows "Équipé" and the equipped styling when already equipped', () => {
    const wrapper = mount(GrimoireSkillCard, {
      props: { skill: skill(), isKnown: true, isEquipped: true, disabled: false },
    });

    expect(wrapper.find('.grimoire-toggle').text()).toBe('Équipé');
    expect(wrapper.classes()).toContain('grimoire-card--equipped');
  });

  it('shows a lock hint with acquisition hints instead of a toggle for an unknown skill', () => {
    const wrapper = mount(GrimoireSkillCard, {
      props: {
        skill: skill({ acquisitionHints: ['Offert par Hitomi'] }),
        isKnown: false,
        isEquipped: false,
        disabled: true,
      },
    });

    expect(wrapper.find('.grimoire-toggle').exists()).toBe(false);
    expect(wrapper.find('.grimoire-lock-hint').text()).toBe('Offert par Hitomi');
    expect(wrapper.classes()).toContain('grimoire-card--locked');
  });

  it('disables the toggle only when not equipped and the loadout is full', () => {
    const wrapper = mount(GrimoireSkillCard, {
      props: { skill: skill(), isKnown: true, isEquipped: false, disabled: true },
    });

    expect(wrapper.find('.grimoire-toggle').attributes('disabled')).toBeDefined();
  });

  it('emits toggleEquip with the skill key when the toggle is clicked', async () => {
    const wrapper = mount(GrimoireSkillCard, {
      props: { skill: skill({ key: 'skill.xyz' }), isKnown: true, isEquipped: false, disabled: false },
    });

    await wrapper.find('.grimoire-toggle').trigger('click');

    expect(wrapper.emitted('toggleEquip')).toEqual([['skill.xyz']]);
  });

  it('shows the complete tactical contract received from the catalog', () => {
    const wrapper = mount(GrimoireSkillCard, {
      props: {
        skill: skill({
          tacticalRange: 4,
          tacticalAreaShape: 'Diamond',
          requiresLineOfSight: true,
          cooldown: 2,
          isUltimate: true,
          emotionalRegister: 'Effroi',
          manaCost: 12,
          chargeCost: 3,
        }),
        isKnown: true,
        isEquipped: false,
        disabled: false,
      },
    });

    expect(wrapper.text()).toContain('Ultime');
    expect(wrapper.text()).toContain('Portée : 4');
    expect(wrapper.text()).toContain('Zone : Diamond');
    expect(wrapper.text()).toContain('Ligne de vue');
    expect(wrapper.text()).toContain('Recharge : 2 tours');
    expect(wrapper.findComponent({ name: 'EmotionalTypeBadge' }).exists()).toBe(true);
  });
});
