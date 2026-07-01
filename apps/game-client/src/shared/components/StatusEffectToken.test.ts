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

  it('renders the lock sigil for AtbLock', () => {
    const wrapper = mount(StatusEffectToken, { props: { kind: 'AtbLock' } });
    expect(wrapper.find('.sigil__tube').exists()).toBe(true);
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
});
