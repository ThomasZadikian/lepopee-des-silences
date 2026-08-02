// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import StatusEffectToken from './StatusEffectToken.vue';

describe('StatusEffectToken', () => {
  it('renders without crashing', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'DamageOverTime' } });
    expect(wrapper.exists()).toBe(true);
  });

  it('renders the drop sigil for DamageOverTime', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'DamageOverTime' } });
    expect(wrapper.find('.sigil__drop').exists()).toBe(true);
  });

  it('renders the plus sigil for HealOverTime', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'HealOverTime' } });
    expect(wrapper.findAll('.sigil__bar').length).toBe(2);
  });

  it('renders an upward chevron for a positive StatModifier', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'StatModifier', magnitude: 5 } });
    expect(wrapper.find('.sigil__chev--ul').exists()).toBe(true);
  });

  it('renders a downward chevron for a negative StatModifier', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'StatModifier', magnitude: -5 } });
    expect(wrapper.find('.sigil__chev--dul').exists()).toBe(true);
  });

  it('renders the aster sigil for Stun', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'Stun' } });
    expect(wrapper.findAll('.sigil__aster').length).toBe(3);
  });

  it('renders the silence sigil for Silence', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'Silence' } });
    expect(wrapper.find('.sigil__ring').exists()).toBe(true);
    expect(wrapper.find('.sigil__slash').exists()).toBe(true);
  });

  it('renders the ring sigil for GuardOverTime', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'GuardOverTime' } });
    expect(wrapper.find('.sigil__ring').exists()).toBe(true);
    expect(wrapper.find('.sigil__slash').exists()).toBe(false);
  });

  it('renders the diamond sigil for SkillGrant', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'SkillGrant' } });
    expect(wrapper.find('.sigil__diamond').exists()).toBe(true);
  });

  it('hides the stack counter when stacks <= 1', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'DamageOverTime', stacks: 1 } });
    expect(wrapper.find('.sigil__stacks').exists()).toBe(false);
  });

  it('shows the stack counter when stacks > 1', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'DamageOverTime', stacks: 3 } });
    expect(wrapper.find('.sigil__stacks').text()).toBe('3');
  });

  it('hides meta label by default', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'DamageOverTime' } });
    expect(wrapper.find('.sigil__meta').exists()).toBe(false);
  });

  it('shows meta label when requested', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'DamageOverTime', meta: true } });
    expect(wrapper.find('.sigil__meta').text()).toBe('Dégât continu');
  });

  it('shows the per-tick amount in the hover bubble for a periodic effect', () => {
    const wrapper = mount(StatusEffectToken, {
      props: { kind: 'DamageOverTime', perTickAmount: 7 },
    });
    expect(wrapper.find('.sigil__bubble').text()).toContain('Dégâts réels : 7 / tour');
  });

  it('does not show a per-tick line for a non-periodic effect', () => {
    const wrapper = mount(StatusEffectToken, {
      props: { kind: 'StatModifier', magnitude: 5, perTickAmount: 0 },
    });
    expect(wrapper.find('.sigil__bubble').text()).not.toContain('/ tour');
  });

  it('shows ticks remaining converted to tours in the hover bubble', () => {
    const wrapper = mount(StatusEffectToken, {
      props: { kind: 'DamageOverTime', ticksRemaining: 5001 },
    });
    expect(wrapper.find('.sigil__bubble').text()).toContain('3 tours restants');
  });

  it('shows a singular tour label when exactly one tour remains', () => {
    const wrapper = mount(StatusEffectToken, {
      props: { kind: 'DamageOverTime', ticksRemaining: 100 },
    });
    expect(wrapper.find('.sigil__bubble').text()).toContain('1 tour restant');
  });

  it('shows a permanent label instead of ticks remaining when isPermanent', () => {
    const wrapper = mount(StatusEffectToken, {
      props: { kind: 'StatModifier', magnitude: 20, isPermanent: true, ticksRemaining: null },
    });
    expect(wrapper.find('.sigil__bubble').text()).toContain('Permanent');
  });

  it('shows the stack count in the hover bubble title when stacked', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'DamageOverTime', stacks: 3 } });
    expect(wrapper.find('.sigil__bubble-title').text()).toBe('Dégât continu ×3');
  });

  // Regression: ticks remaining used to be visible only in the hover bubble,
  // requiring a hover/click to see how long a DoT/status has left.
  it('shows a persistent ticks-remaining badge without needing to hover', () => {
    const wrapper = mount(StatusEffectToken, {
      props: { kind: 'DamageOverTime', ticksRemaining: 5001 },
    });
    expect(wrapper.find('.sigil__ticks').text()).toBe('3');
  });

  it('hides the ticks-remaining badge when the effect is permanent', () => {
    const wrapper = mount(StatusEffectToken, {
      props: { kind: 'StatModifier', magnitude: 20, isPermanent: true, ticksRemaining: null },
    });
    expect(wrapper.find('.sigil__ticks').exists()).toBe(false);
  });

  it('hides the ticks-remaining badge when ticksRemaining is unknown', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'DamageOverTime' } });
    expect(wrapper.find('.sigil__ticks').exists()).toBe(false);
  });

  it('shows the affected stat and flat amount in the hover bubble for a StatModifier', () => {
    const wrapper = mount(StatusEffectToken, {
      props: { kind: 'StatModifier', magnitude: -8, stat: 'Defense' },
    });
    expect(wrapper.find('.sigil__bubble').text()).toContain('Défense : -8');
  });

  it('shows the percent unit when the StatModifier magnitude is a percent of base stat', () => {
    const wrapper = mount(StatusEffectToken, {
      props: {
        kind: 'StatModifier', magnitude: 15, stat: 'AttackPower',
        isMagnitudePercentOfBaseStat: true,
      },
    });
    expect(wrapper.find('.sigil__bubble').text()).toContain('Attaque : +15%');
  });

  it('does not show a stat line when stat is missing or None', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'StatModifier', magnitude: 5 } });
    expect(wrapper.find('.sigil__bubble').text()).not.toContain(':');
  });
});
