// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import CombatantCard from './CombatantCard.vue';
import type { CombatantRuntimeDto } from '../types/combatContracts';

function makeCombatant(overrides: Partial<CombatantRuntimeDto> = {}): CombatantRuntimeDto {
  return {
    id: 'ally-1',
    sourceKey: 'player.self',
    displayName: 'Hero',
    side: 'Player',
    archetype: 'Fighter',
    maxVitality: 100,
    currentVitality: 80,
    guard: 10,
    mana: 5,
    charge: 0,
    status: 'Active',
    attackType: 'Effroi',
    skills: [],
    ...overrides,
  };
}

function mountCard(combatant?: CombatantRuntimeDto, stateOverrides = {}) {
  const c = combatant ?? makeCombatant();
  return mount(CombatantCard, {
    props: {
      combatant: c,
      isCurrentActor: false,
      isSelectedTarget: false,
      isSelectable: true,
      isTargetable: true,
      isInvalidTarget: false,
      isActivePlayer: false,
      isThinking: false,
      isDamaged: false,
      isGuarded: false,
      isJustDefeated: false,
      isActing: false,
      ...stateOverrides,
    },
    global: {
      stubs: {
        AtbGauge: { template: '<div class="atb" />' },
        EmotionalTypeBadge: { template: '<span class="type-badge" />' },
      },
    },
  });
}

describe('CombatantCard', () => {
  it('renders without crashing', () => {
    const wrapper = mountCard();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the combatant name', () => {
    const wrapper = mountCard(makeCombatant({ displayName: 'Warrior' }));
    expect(wrapper.find('.presence__name').text()).toBe('Warrior');
  });

  it('displays vitality stats', () => {
    const wrapper = mountCard(makeCombatant({ currentVitality: 75, maxVitality: 100 }));
    expect(wrapper.text()).toContain('PV 75 / 100');
  });

  it('displays guard stat when guard > 0', () => {
    const wrapper = mountCard(makeCombatant({ guard: 15 }));
    expect(wrapper.text()).toContain('15');
  });

  it('displays mana stat for player side', () => {
    const wrapper = mountCard(makeCombatant({ side: 'Player', mana: 10 }));
    expect(wrapper.text()).toContain('10 souffle');
  });

  it('does not display mana stat for enemy side', () => {
    const wrapper = mountCard(makeCombatant({ side: 'Enemy' }));
    expect(wrapper.text()).not.toContain('souffle');
  });

  it('applies presence--active class when isCurrentActor is true', () => {
    const wrapper = mountCard(undefined, { isCurrentActor: true });
    expect(wrapper.find('.presence').classes()).toContain('presence--active');
  });

  it('applies presence--selected class when isSelectedTarget is true', () => {
    const wrapper = mountCard(undefined, { isSelectedTarget: true });
    expect(wrapper.find('.presence').classes()).toContain('presence--selected');
  });

  it('applies presence--selectable class when isSelectable is true', () => {
    const wrapper = mountCard(undefined, { isSelectable: true });
    expect(wrapper.find('.presence').classes()).toContain('presence--selectable');
  });

  it('applies presence--invalid class when isInvalidTarget is true', () => {
    const wrapper = mountCard(undefined, { isInvalidTarget: true });
    expect(wrapper.find('.presence').classes()).toContain('presence--invalid');
  });

  it('applies presence--thinking class when isThinking is true', () => {
    const wrapper = mountCard(undefined, { isThinking: true });
    expect(wrapper.find('.presence').classes()).toContain('presence--thinking');
  });

  it('applies presence--acting class when isActing is true', () => {
    const wrapper = mountCard(undefined, { isActing: true });
    expect(wrapper.find('.presence').classes()).toContain('presence--acting');
  });

  it('applies presence--damaged class when isDamaged is true', () => {
    const wrapper = mountCard(undefined, { isDamaged: true });
    expect(wrapper.find('.presence').classes()).toContain('presence--damaged');
  });

  it('applies presence--guarded class when isGuarded is true', () => {
    const wrapper = mountCard(undefined, { isGuarded: true });
    expect(wrapper.find('.presence').classes()).toContain('presence--guarded');
  });

  it('applies presence--defeated class when status is Defeated', () => {
    const wrapper = mountCard(makeCombatant({ status: 'Defeated' }));
    expect(wrapper.find('.presence').classes()).toContain('presence--defeated');
  });

  it('applies presence--just-defeated class when isJustDefeated is true', () => {
    const wrapper = mountCard(undefined, { isJustDefeated: true });
    expect(wrapper.find('.presence').classes()).toContain('presence--just-defeated');
  });

  it('applies presence--ally class for player side', () => {
    const wrapper = mountCard(makeCombatant({ side: 'Player' }));
    expect(wrapper.find('.presence').classes()).toContain('presence--ally');
  });

  it('applies presence--enemy class for enemy side', () => {
    const wrapper = mountCard(makeCombatant({ side: 'Enemy' }));
    expect(wrapper.find('.presence').classes()).toContain('presence--enemy');
  });

  it('applies presence--active-player class when isActivePlayer is true', () => {
    const wrapper = mountCard(undefined, { isActivePlayer: true });
    expect(wrapper.find('.presence').classes()).toContain('presence--active-player');
  });

  it('shows PRÊT state when isActivePlayer is true', () => {
    const wrapper = mountCard(undefined, { isActivePlayer: true });
    expect(wrapper.find('.presence__state--ready').text()).toBe('PRÊT');
  });

  it('shows cible state when isSelectedTarget is true and not active player', () => {
    const wrapper = mountCard(undefined, { isSelectedTarget: true, isActivePlayer: false });
    expect(wrapper.find('.presence__state--target').text()).toBe('cible');
  });

  it('shows abattu state when defeated', () => {
    const wrapper = mountCard(makeCombatant({ status: 'Defeated' }));
    expect(wrapper.find('.presence__state--dead').text()).toBe('abattu');
  });

  it('shows guard tag when guard > 0', () => {
    const wrapper = mountCard(makeCombatant({ guard: 20 }));
    expect(wrapper.find('.presence__tag--guard').text()).toBe('Garde');
  });

  it('disables button when defeated', () => {
    const wrapper = mountCard(makeCombatant({ status: 'Defeated' }));
    expect((wrapper.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('disables button when not selectable', () => {
    const wrapper = mountCard(undefined, { isSelectable: false });
    expect((wrapper.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('emits select with combatant id on click', async () => {
    const wrapper = mountCard(makeCombatant({ id: 'combatant-42' }));
    await wrapper.trigger('click');
    const events = wrapper.emitted('select') as string[][];
    expect(events).toBeDefined();
    expect(events[0][0]).toBe('combatant-42');
  });

  it('does not emit select when disabled', async () => {
    const wrapper = mountCard(makeCombatant({ status: 'Defeated' }));
    await wrapper.trigger('click');
    expect(wrapper.emitted('select')).toBeUndefined();
  });

  it('renders status effects badges', () => {
    const wrapper = mountCard(makeCombatant({
      statusEffects: [
        { key: 'dot-1', displayName: 'Poison', kind: 'DamageOverTime', stat: '', magnitude: 5, stacks: 1 },
      ],
    }));
    expect(wrapper.find('.presence__fx-badge').exists()).toBe(true);
  });

  it('shows stack count when stacks > 1', () => {
    const wrapper = mountCard(makeCombatant({
      statusEffects: [
        { key: 'buff-1', displayName: 'Power Up', kind: 'StatModifier', stat: 'attack', magnitude: 10, stacks: 3 },
      ],
    }));
    expect(wrapper.find('.presence__fx-badge small').text()).toBe('3');
  });

  it('does not render status effects section when no effects', () => {
    const wrapper = mountCard(makeCombatant({ statusEffects: [] }));
    expect(wrapper.find('.presence__fx').exists()).toBe(false);
  });

  it('renders hp gauge fill with correct width', () => {
    const wrapper = mountCard(makeCombatant({ currentVitality: 50, maxVitality: 100 }));
    const fill = wrapper.find('.presence__gauge-fill');
    expect(fill.attributes('style')).toContain('50%');
  });

  it('renders substats', () => {
    const wrapper = mountCard(makeCombatant({
      attackPower: 25,
      defense: 15,
      speed: 20,
      focus: 10,
    }));
    expect(wrapper.text()).toContain('25');
    expect(wrapper.text()).toContain('15');
    expect(wrapper.text()).toContain('20');
    expect(wrapper.text()).toContain('10');
  });

  it('handles zero maxVitality without crashing', () => {
    const wrapper = mountCard(makeCombatant({ maxVitality: 0, currentVitality: 0 }));
    expect(wrapper.exists()).toBe(true);
  });
});
