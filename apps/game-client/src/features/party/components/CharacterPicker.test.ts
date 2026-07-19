// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';

import CharacterPicker from './CharacterPicker.vue';
import type { PlayerCharacterView } from '../types/playerTypes';

function character(overrides: Partial<PlayerCharacterView> = {}): PlayerCharacterView {
  return {
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
      speed: 10, initiative: 10, recovery: 5, focus: 0, mana: 0, charge: 0,
    },
    ...overrides,
  };
}

describe('CharacterPicker', () => {
  it('renders nothing when there is only one character', () => {
    const wrapper = mount(CharacterPicker, {
      props: { characters: [character()], modelValue: 'char-1' },
    });

    expect(wrapper.find('.character-picker').exists()).toBe(false);
  });

  it('renders one chip per character and highlights the selected one', () => {
    const companion = character({ id: 'char-2', displayName: 'Mané', characterType: 'Companion' });
    const wrapper = mount(CharacterPicker, {
      props: { characters: [character(), companion], modelValue: 'char-2' },
    });

    const chips = wrapper.findAll('.character-picker__chip');
    expect(chips).toHaveLength(2);
    expect(chips[1].classes()).toContain('character-picker__chip--active');
    expect(chips[0].classes()).not.toContain('character-picker__chip--active');
    expect(chips[1].text()).toContain('Compagnon');
  });

  it('emits update:modelValue when a chip is clicked', async () => {
    const companion = character({ id: 'char-2', displayName: 'Mané', characterType: 'Companion' });
    const wrapper = mount(CharacterPicker, {
      props: { characters: [character(), companion], modelValue: 'char-1' },
    });

    await wrapper.findAll('.character-picker__chip')[1].trigger('click');

    expect(wrapper.emitted('update:modelValue')).toEqual([['char-2']]);
  });
});
