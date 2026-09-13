// @vitest-environment jsdom
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

import CombatDevToolsWindow from './CombatDevToolsWindow.vue';

const combat = {
  id: 'combat-1',
  status: 'Active',
  activeCombatantId: 'ally-1',
  allies: [{
    x: 1, y: 1, hasMoved: false, hasActed: false, movementBudget: 4,
    facing: 'North', skillCooldowns: {},
    combatant: {
      id: 'ally-1', displayName: 'Écrivain', side: 'Player',
      currentVitality: 80, maxVitality: 100, guard: 12,
    },
  }],
  enemies: [{
    x: 4, y: 1, hasMoved: false, hasActed: false, movementBudget: 4,
    facing: 'South', skillCooldowns: {},
    combatant: {
      id: 'enemy-1', displayName: 'Ombre', side: 'Enemy',
      currentVitality: 40, maxVitality: 60, guard: 0,
    },
  }],
} as any;

describe('CombatDevToolsWindow', () => {
  it('explains when there is no active combat', () => {
    const wrapper = mount(CombatDevToolsWindow, {
      props: { disabled: false, isLoading: false, combat: null },
    });

    expect(wrapper.text()).toContain('Aucun combat actif');
  });

  it('starts a standard combat scenario at the selected risk tier', async () => {
    const wrapper = mount(CombatDevToolsWindow, {
      props: { disabled: false, isLoading: false, combat: null },
    });

    await wrapper.get('[data-testid="combat-risk-tier"]').setValue('Fatal');
    await wrapper.get('[data-testid="start-combat-scenario"]').trigger('click');

    expect(wrapper.emitted('startScenario')).toEqual([['Fatal']]);
  });

  it('emits combat mutations for the selected combatant', async () => {
    const wrapper = mount(CombatDevToolsWindow, {
      props: { disabled: false, isLoading: false, combat },
    });

    await wrapper.get('[data-testid="combatant-enemy-1"]').trigger('click');
    await wrapper.get('[data-testid="kill-combatant"]').trigger('click');
    await wrapper.get('[data-testid="apply-status"]').trigger('click');

    expect(wrapper.emitted('killEnemy')).toEqual([['enemy-1']]);
    expect(wrapper.emitted('applyStatus')).toEqual([['enemy-1', 'poison', 1, 6]]);
  });

  it('emits edited vitality and guard values', async () => {
    const wrapper = mount(CombatDevToolsWindow, {
      props: { disabled: false, isLoading: false, combat },
    });

    await wrapper.get('[data-testid="vitality-input"]').setValue(55);
    await wrapper.get('[data-testid="guard-input"]').setValue(7);
    await wrapper.get('[data-testid="set-vitals"]').trigger('click');

    expect(wrapper.emitted('setVitals')).toEqual([['ally-1', 55, 7]]);
  });
});
