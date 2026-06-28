// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import CombatMetersPanel from './CombatMetersPanel.vue';
import type { CombatRuntimeDto } from '../types/combatContracts';
import type { CombatMetricsState } from '../composables/useCombatMetrics';

function makeCombatant(id: string, displayName: string, side: 'Player' | 'Enemy'): CombatRuntimeDto['allies'][number] {
  return {
    id,
    sourceKey: `source.${id}`,
    displayName,
    side,
    archetype: 'Fighter',
    maxVitality: 100,
    currentVitality: 100,
    guard: 0,
    mana: 0,
    charge: 0,
    status: 'Active',
    skills: [],
  };
}

function makeCombat(): CombatRuntimeDto {
  return {
    id: 'combat-1',
    status: 'Active',
    turnNumber: 3,
    activeCombatantId: 'ally-1',
    allies: [makeCombatant('ally-1', 'Hero', 'Player'), makeCombatant('ally-2', 'Mage', 'Player')],
    enemies: [makeCombatant('enemy-1', 'Beast', 'Enemy')],
    usableBattleItems: [],
  };
}

function makeMetrics(): CombatMetricsState {
  return {
    combatId: 'combat-1',
    allies: { damageDealt: 0, damageTaken: 0, healingDone: 0, healingReceived: 0, guardAbsorbed: 0, guardGained: 0, netVitalityLoss: 0 },
    enemies: { damageDealt: 0, damageTaken: 0, healingDone: 0, healingReceived: 0, guardAbsorbed: 0, guardGained: 0, netVitalityLoss: 0 },
    contributions: {
      'ally-1': { id: 'ally-1', side: 'Player', damageDealt: 30, damageTaken: 10, guardAbsorbed: 5, healingReceived: 0, healingDone: 0, guardGained: 0, turnsTaken: 2, damageToGuard: 10 },
      'ally-2': { id: 'ally-2', side: 'Player', damageDealt: 15, damageTaken: 5, guardAbsorbed: 0, healingReceived: 10, healingDone: 10, guardGained: 0, turnsTaken: 1, damageToGuard: 0 },
      'enemy-1': { id: 'enemy-1', side: 'Enemy', damageDealt: 15, damageTaken: 45, guardAbsorbed: 0, healingReceived: 0, healingDone: 0, guardGained: 0, turnsTaken: 1, damageToGuard: 0 },
    },
    floatEvents: [],
  };
}

function mountPanel(metrics: CombatMetricsState, combat: CombatRuntimeDto | null) {
  return mount(CombatMetersPanel, { props: { metrics, combat } });
}

describe('CombatMetersPanel', () => {
  it('renders without crashing', () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the turn number', () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    expect(wrapper.text()).toContain('3');
  });

  it('shows dash when combat is null', () => {
    const wrapper = mountPanel(makeMetrics(), null);
    expect(wrapper.text()).toContain('—');
  });

  it('displays ally count', () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    expect(wrapper.text()).toContain('Alliés · 2');
  });

  it('displays enemy count', () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    expect(wrapper.text()).toContain('Les Manifestations · 1');
  });

  it('shows default metric as damage dealt', () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    expect(wrapper.text()).toContain('Dégâts infligés');
  });

  it('renders rows sorted by value descending', () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    const rows = wrapper.findAll('.damage-row');
    expect(rows.length).toBeGreaterThanOrEqual(2);
    const firstRank = rows[0].find('.damage-row__rank').text();
    expect(firstRank).toBe('1');
  });

  it('displays combatant names in rows', () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    const names = wrapper.findAll('.damage-row__name').map((el) => el.text());
    expect(names).toContain('Hero');
    expect(names).toContain('Mage');
  });

  it('displays values in rows', () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    const values = wrapper.findAll('.damage-row__value');
    expect(values.length).toBeGreaterThan(0);
  });

  it('opens ally metric dropdown on trigger click', async () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    const triggers = wrapper.findAll('.ms__trigger');
    await triggers[0].trigger('click');
    expect(wrapper.findAll('.ms__panel').length).toBeGreaterThanOrEqual(1);
  });

  it('shows metric options in dropdown', async () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    const triggers = wrapper.findAll('.ms__trigger');
    await triggers[0].trigger('click');
    const options = wrapper.findAll('.ms__option');
    expect(options.length).toBe(6);
  });

  it('selects a new metric on option click', async () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    const triggers = wrapper.findAll('.ms__trigger');
    await triggers[0].trigger('click');
    const options = wrapper.findAll('.ms__option');
    await options[1].trigger('click');
    expect(wrapper.text()).toContain('Dégâts subis');
  });

  it('closes dropdown when selecting a metric', async () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    const triggers = wrapper.findAll('.ms__trigger');
    await triggers[0].trigger('click');
    const options = wrapper.findAll('.ms__option');
    await options[1].trigger('click');
    expect(wrapper.findAll('.ms__panel').length).toBe(0);
  });

  it('shows total in footer', () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    expect(wrapper.text()).toContain('Total');
  });

  it('renders bar fills with width styles', () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    const fills = wrapper.findAll('.damage-row__bar-fill');
    expect(fills.length).toBeGreaterThan(0);
    fills.forEach((fill) => {
      expect(fill.attributes('style')).toBeDefined();
    });
  });

  it('applies correct bar fill class for damageDealt', () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    const fills = wrapper.findAll('.damage-row__bar-fill');
    const hasBlood = fills.some((f) => f.classes().includes('bar-fill--blood'));
    expect(hasBlood).toBe(true);
  });

  it('renders empty rows when no combatants', () => {
    const emptyCombat = { ...makeCombat(), allies: [], enemies: [] };
    const wrapper = mountPanel(makeMetrics(), emptyCombat);
    const allyRows = wrapper.find('.damage-report__side--allies').findAll('.damage-row');
    expect(allyRows.length).toBe(0);
  });

  it('shows footer description text', () => {
    const wrapper = mountPanel(makeMetrics(), makeCombat());
    expect(wrapper.text()).toContain('Dégâts portés à travers la faille');
  });
});
