// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import CombatDevToolsSection from './CombatDevToolsSection.vue';
import type { CombatRuntimeDto } from '../../combat/types/combatContracts';

function mountSection(
  combat: CombatRuntimeDto | null = null,
  disabled = false,
  isLoading = false,
) {
  return mount(CombatDevToolsSection, {
    props: { combat, disabled, isLoading },
  });
}

describe('CombatDevToolsSection', () => {
  const baseCombat: CombatRuntimeDto = {
    id: 'combat-1',
    status: 'Active',
    turnNumber: 1,
    activeCombatantId: 'ally-1',
    allies: [
      {
        id: 'ally-1',
        sourceKey: 'player.self',
        displayName: 'Hero',
        side: 'Player',
        archetype: 'Fighter',
        maxVitality: 100,
        currentVitality: 100,
        guard: 0,
        mana: 0,
        charge: 0,
        status: 'Active',
        skills: [],
      },
    ],
    enemies: [
      {
        id: 'enemy-1',
        sourceKey: 'enemy.test',
        displayName: 'Enemy',
        side: 'Enemy',
        archetype: 'Guard',
        maxVitality: 30,
        currentVitality: 30,
        guard: 0,
        mana: 0,
        charge: 0,
        status: 'Active',
        skills: [],
      },
    ],
    usableBattleItems: [],
  };

  it('renders without crashing', () => {
    const wrapper = mountSection();
    expect(wrapper.exists()).toBe(true);
  });

  it('shows muted message when no combat', () => {
    const wrapper = mountSection();
    expect(wrapper.text()).toContain('aucun combat actif');
  });

  it('hides combat controls when no combat', () => {
    const wrapper = mountSection();
    expect(wrapper.find('.devtools-vitals-grid').exists()).toBe(false);
  });

  it('shows kill all enemies button when combat exists', () => {
    const wrapper = mountSection(baseCombat);
    expect(wrapper.text()).toContain('Kill all enemies');
  });

  it('shows enemy selector', () => {
    const wrapper = mountSection(baseCombat);
    expect(wrapper.text()).toContain('Choisir un ennemi');
    expect(wrapper.text()).toContain('Enemy');
  });

  it('shows combatant selector', () => {
    const wrapper = mountSection(baseCombat);
    expect(wrapper.text()).toContain('Choisir un combatant');
    expect(wrapper.text()).toContain('Hero');
  });

  it('emits killEnemies on confirm', async () => {
    const wrapper = mountSection(baseCombat);
    // Mock window.confirm
    const originalConfirm = globalThis.window.confirm;
    globalThis.window.confirm = vi.fn(() => true);

    const btn = wrapper.findAll('button').find((b) => b.text().includes('Kill all enemies'));
    if (btn) await btn.trigger('click');

    expect(wrapper.emitted('killEnemies')).toHaveLength(1);
    globalThis.window.confirm = originalConfirm;
  });

  it('emits killEnemy with selected enemy id', async () => {
    const wrapper = mountSection(baseCombat);
    const select = wrapper.findAll('select')[0];
    await select.setValue('enemy-1');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Kill enemy'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('killEnemy')).toBeDefined();
    expect(wrapper.emitted('killEnemy')![0][0]).toBe('enemy-1');
  });

  it('emits setVitals with selected values', async () => {
    const wrapper = mountSection(baseCombat);
    const selects = wrapper.findAll('select');
    await selects[1].setValue('ally-1');
    const inputs = wrapper.findAll('input[type="number"]');
    await inputs[0].setValue(50);
    await inputs[1].setValue(10);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Set vitals'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('setVitals')).toBeDefined();
    expect(wrapper.emitted('setVitals')![0]).toEqual(['ally-1', 50, 10]);
  });

  it('emits applyStatus with selected values', async () => {
    const wrapper = mountSection(baseCombat);
    const select = wrapper.findAll('select')[1];
    await select.setValue('ally-1');
    const inputs = wrapper.findAll('input');
    const statusInput = inputs.find((i) => i.attributes('placeholder') === 'statusKey');
    if (statusInput) await statusInput.setValue('poison');
    const stackInputs = wrapper.findAll('input[type="number"]');
    if (stackInputs.length >= 4) {
      await stackInputs[2].setValue(3);
      await stackInputs[3].setValue(2);
    }
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Apply status'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('applyStatus')).toBeDefined();
  });

  it('disables buttons when disabled prop is true', () => {
    const wrapper = mountSection(baseCombat, true);
    const buttons = wrapper.findAll('button');
    const disabledButtons = buttons.filter((b) => (b.element as HTMLButtonElement).disabled);
    expect(disabledButtons.length).toBeGreaterThan(0);
  });

  it('disables buttons when isLoading is true', () => {
    const wrapper = mountSection(baseCombat, false, true);
    const buttons = wrapper.findAll('button');
    const disabledButtons = buttons.filter((b) => (b.element as HTMLButtonElement).disabled);
    expect(disabledButtons.length).toBeGreaterThan(0);
  });

  it('clamps vitality values', async () => {
    const wrapper = mountSection(baseCombat);
    const select = wrapper.findAll('select')[1];
    await select.setValue('ally-1');
    const inputs = wrapper.findAll('input[type="number"]');
    await inputs[0].setValue(-10);
    await inputs[1].setValue(2000);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Set vitals'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('setVitals')![0]).toEqual(['ally-1', 0, 999]);
  });

  it('does not emit setVitals without selected combatant', async () => {
    const wrapper = mountSection(baseCombat);
    const inputs = wrapper.findAll('input[type="number"]');
    await inputs[0].setValue(50);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Set vitals'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('setVitals')).toBeUndefined();
  });

  it('does not emit killEnemy without selected enemy', async () => {
    const wrapper = mountSection(baseCombat);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Kill enemy'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('killEnemy')).toBeUndefined();
  });
});
