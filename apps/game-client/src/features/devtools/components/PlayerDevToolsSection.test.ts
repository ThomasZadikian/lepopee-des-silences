// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import PlayerDevToolsSection from './PlayerDevToolsSection.vue';
import type { PlayerCharacterView } from '../../party/types/playerTypes';
import type { SkillDefinitionView } from '../../party/types/skillTypes';

const baseCharacter: PlayerCharacterView = {
  id: 'char-1',
  definitionKey: 'character.player.self',
  displayName: 'Le Porteur',
  maxEquippedSkills: 4,
  items: [],
  maxEquippedItems: 3,
  characterType: 'Standard',
  skills: [],
  stats: {
    maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
    speed: 10, initiative: 10,focus: 0, mana: 0, charge: 0,
  },
};

const baseSkills: SkillDefinitionView[] = [
  { key: 'skill.a', displayName: 'Frappe', description: 'Un coup.', skillType: 'Damage', targetingType: 'SingleEnemy', effectType: 'Damage', manaCost: 0, chargeCost: 0, basePower: 10 },
  { key: 'skill.b', displayName: 'Garde', description: 'Se protège.', skillType: 'Guard', targetingType: 'Self', effectType: 'Buff', manaCost: 0, chargeCost: 0, basePower: 0 },
];

function mountSection(
  characters: PlayerCharacterView[] = [baseCharacter],
  allSkills: SkillDefinitionView[] = baseSkills,
  disabled = false,
  isLoading = false,
) {
  return mount(PlayerDevToolsSection, {
    props: { disabled, isLoading, characters, allSkills },
  });
}

describe('PlayerDevToolsSection', () => {
  it('renders without crashing', () => {
    const wrapper = mountSection();
    expect(wrapper.exists()).toBe(true);
  });

  it('lists every skill as an option', () => {
    const wrapper = mountSection();
    const options = wrapper.findAll('select')[0].findAll('option');
    expect(options.map((o) => o.text())).toContain('Frappe');
    expect(options.map((o) => o.text())).toContain('Garde');
  });

  it('does not show a character picker with a single character', () => {
    const wrapper = mountSection([baseCharacter]);
    expect(wrapper.findAll('select')).toHaveLength(1);
  });

  it('shows a character picker with multiple characters', () => {
    const other = { ...baseCharacter, id: 'char-2', displayName: 'Compagnon' };
    const wrapper = mountSection([baseCharacter, other]);
    expect(wrapper.findAll('select')).toHaveLength(2);
  });

  it('emits unlockSkill with the default character when only one exists', async () => {
    const wrapper = mountSection();
    const select = wrapper.find('select');
    await select.setValue('skill.b');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Débloquer'));
    await btn!.trigger('click');
    expect(wrapper.emitted('unlockSkill')).toEqual([['char-1', 'skill.b']]);
  });

  it('does not emit unlockSkill without a selected skill', async () => {
    const wrapper = mountSection();
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Débloquer'));
    expect((btn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('emits awardStatPoints with the entered amount', async () => {
    const wrapper = mountSection();
    const input = wrapper.find('input[type="number"]');
    await input.setValue(5);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Points de compétence'));
    await btn!.trigger('click');
    expect(wrapper.emitted('awardStatPoints')).toEqual([[5]]);
  });

  it('clamps the stat point amount between 1 and 20', async () => {
    const wrapper = mountSection();
    const input = wrapper.find('input[type="number"]');
    await input.setValue(500);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Points de compétence'));
    await btn!.trigger('click');
    expect(wrapper.emitted('awardStatPoints')).toEqual([[20]]);
  });

  it('disables buttons when disabled prop is true', () => {
    const wrapper = mountSection([baseCharacter], baseSkills, true);
    const buttons = wrapper.findAll('button');
    expect(buttons.every((b) => (b.element as HTMLButtonElement).disabled)).toBe(true);
  });

  it('disables the unlock button when there are no characters', () => {
    const wrapper = mountSection([]);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Débloquer'));
    expect((btn!.element as HTMLButtonElement).disabled).toBe(true);
  });
});
