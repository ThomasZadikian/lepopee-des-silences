// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import SkillsDevToolsWindow from './SkillsDevToolsWindow.vue';
import type { PlayerCharacterView } from '../../party/types/playerTypes';
import type { SkillDefinitionView } from '../../party/types/skillTypes';

const characters: PlayerCharacterView[] = [
  {
    id: 'char-1', definitionKey: 'def.hero', displayName: 'Héros', skills: [],
    stats: {} as PlayerCharacterView['stats'], maxEquippedSkills: 4, items: [],
    maxEquippedItems: 4, characterType: 'Standard',
  },
  {
    id: 'char-2', definitionKey: 'def.thomas', displayName: 'Thomas', skills: [],
    stats: {} as PlayerCharacterView['stats'], maxEquippedSkills: 4, items: [],
    maxEquippedItems: 4, characterType: 'Companion',
  },
];

const allSkills: SkillDefinitionView[] = [
  {
    key: 'canon.skill.strike', displayName: 'Frappe', description: 'Un coup simple.',
    skillType: 'Damage', targetingType: 'SingleEnemy', effectType: 'Damage',
    manaCost: 2, chargeCost: 0, basePower: 10, category: 'Physical',
    basePowerIsPercentOfMaxVitality: false, effects: [], acquisitionHints: [],
  },
  {
    key: 'canon.skill.ultimate', displayName: 'Ultime', description: 'Un coup dévastateur.',
    skillType: 'Damage', targetingType: 'SingleEnemy', effectType: 'Damage',
    manaCost: 20, chargeCost: 5, basePower: 100, category: 'Magic',
    basePowerIsPercentOfMaxVitality: false, effects: [], acquisitionHints: [], isUltimate: true,
  },
];

function mountWindow(disabled = false, isLoading = false, chars = characters) {
  return mount(SkillsDevToolsWindow, {
    props: { disabled, isLoading, characters: chars, allSkills },
  });
}

describe('SkillsDevToolsWindow', () => {
  it('lists every skill in the catalog grid', () => {
    const wrapper = mountWindow();
    expect(wrapper.text()).toContain('Frappe');
    expect(wrapper.text()).toContain('Ultime');
  });

  it('filters skills by search query', async () => {
    const wrapper = mountWindow();
    await wrapper.find('input.devtools-input').setValue('ultime');
    expect(wrapper.text()).toContain('Ultime');
    expect(wrapper.text()).not.toContain('Frappe');
  });

  it('shows the description sheet only once a skill is selected', async () => {
    const wrapper = mountWindow();
    expect(wrapper.text()).not.toContain('Un coup simple.');
    const cell = wrapper.findAll('.devtools-catalog-cell').find((c) => c.text().includes('Frappe'));
    await cell!.trigger('click');
    expect(wrapper.text()).toContain('Un coup simple.');
  });

  it('emits unlockSkill with the selected character and skill key', async () => {
    const wrapper = mountWindow();
    const cell = wrapper.findAll('.devtools-catalog-cell').find((c) => c.text().includes('Frappe'));
    await cell!.trigger('click');

    const select = wrapper.find('select');
    await select.setValue('char-2');

    const btn = wrapper.findAll('button').find((b) => b.text().includes('Débloquer'));
    await btn!.trigger('click');

    expect(wrapper.emitted('unlockSkill')).toEqual([['char-2', 'canon.skill.strike']]);
  });

  it('does not show a character selector when there is only one character', () => {
    const wrapper = mountWindow(false, false, [characters[0]!]);
    expect(wrapper.find('select').exists()).toBe(false);
  });

  it('disables the unlock button while no skill is selected', () => {
    const wrapper = mountWindow();
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Débloquer'));
    expect(btn).toBeUndefined();
  });
});
