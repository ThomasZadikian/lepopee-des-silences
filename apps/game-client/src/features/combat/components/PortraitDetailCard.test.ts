// @vitest-environment jsdom
import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it } from 'vitest';

import { useEmotionalRegisterCatalog } from '../../emotional-registers/store';
import PortraitDetailCard from './PortraitDetailCard.vue';

beforeEach(() => {
  useEmotionalRegisterCatalog().install('test-1', [
    {
      code: 'memoire', displayName: 'Mémoire', glyph: '◈', color: 'gold',
      incomingAffinities: [
        { incomingRegister: 'memoire', outcome: 'Neutral', multiplier: 1 },
        { incomingRegister: 'deni', outcome: 'Weak', multiplier: 1.5 },
      ],
    },
    {
      code: 'deni', displayName: 'Déni', glyph: '◇', color: 'yellow',
      incomingAffinities: [
        { incomingRegister: 'memoire', outcome: 'Neutral', multiplier: 1 },
        { incomingRegister: 'deni', outcome: 'Neutral', multiplier: 1 },
      ],
    },
  ]);
});

describe('PortraitDetailCard emotional affinities', () => {
  it('renders server-resolved outcomes and local multipliers', () => {
    const wrapper = mount(PortraitDetailCard, {
      props: {
        detail: {
          displayedVitality: 100,
          vitalityPercent: 100,
          guardPercent: 0,
          statusEffects: [],
          unit: {
            x: 1, y: 2, hasMoved: false, hasActed: false, movementBudget: 4,
            facing: 'North', skillCooldowns: {},
            combatant: {
              id: 'combatant-1', sourceKey: 'enemy.memory', displayName: 'Mémoire hostile',
              side: 'Enemy', archetype: 'Caster', maxVitality: 100, currentVitality: 100,
              guard: 0, mana: 0, maxMana: 0, charge: 0, status: 'Active', skills: [],
              naturalEmotionalRegister: 'memoire', effectiveAttackRegister: 'memoire',
              incomingAffinities: [
                {
                  incomingRegister: 'memoire', outcome: 'Neutral', baseMultiplier: 1,
                  modifierPercent: 0, effectiveMultiplier: 1, modifiers: [],
                },
                {
                  incomingRegister: 'deni', outcome: 'Weak', baseMultiplier: 1.5,
                  modifierPercent: -20, effectiveMultiplier: 1.2,
                  modifiers: [{
                    sourceKey: 'item.memory-ward', incomingRegister: 'deni', outcomeOverride: null,
                    multiplierPercent: -20, priority: 0, remainingActivations: null,
                  }],
                },
              ],
            },
          },
        },
      },
      global: { stubs: { Teleport: true } },
    });

    expect(wrapper.text()).toContain('Registre naturel');
    expect(wrapper.text()).toContain('Mémoire');
    expect(wrapper.text()).toContain('Faible à');
    expect(wrapper.text()).toContain('1.20×');
  });
});
