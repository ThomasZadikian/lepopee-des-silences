// @vitest-environment jsdom
import { beforeEach, describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import EmotionalTypeBadge from './EmotionalTypeBadge.vue';
import { useEmotionalRegisterCatalog } from '../../emotional-registers/store';

const definitionRows = [
  ['neutral', 'Neutral', '·', 'gray'],
  ['effroi', 'Effroi', '✶', 'red'],
  ['deni', 'Déni', '◇', 'yellow'],
  ['melancolie', 'Mélancolie', '❍', 'blue'],
  ['rupture', 'Rupture', '⟡', 'orange'],
  ['memoire', 'Mémoire', '◈', 'gold'],
  ['silence', 'Silence', '○', 'silver'],
  ['folie', 'Folie', '✳', 'purple'],
];
const definitions = definitionRows.map(([code, displayName, glyph, color]) => ({
  code, displayName, glyph, color,
  incomingAffinities: definitionRows.map(([incomingRegister]) => ({
    incomingRegister, outcome: 'Neutral' as const, multiplier: 1,
  })),
}));

beforeEach(() => {
  useEmotionalRegisterCatalog().install('test-1', definitions);
});

function mountBadge(type: string) {
  return mount(EmotionalTypeBadge, { props: { type } });
}

describe('EmotionalTypeBadge', () => {
  it('renders without crashing', () => {
    const wrapper = mountBadge('Effroi');
    expect(wrapper.exists()).toBe(true);
  });

  it('renders the explicit Neutral type', () => {
    const wrapper = mountBadge('Neutral');
    expect(wrapper.find('.type-badge__label').text()).toBe('Neutral');
  });

  it('renders for Effroi type', () => {
    const wrapper = mountBadge('Effroi');
    expect(wrapper.find('.type-badge').exists()).toBe(true);
    expect(wrapper.find('.type-badge__label').text()).toBe('Effroi');
  });

  it('renders for Deni type', () => {
    const wrapper = mountBadge('Deni');
    expect(wrapper.find('.type-badge__label').text()).toBe('Déni');
  });

  it('renders for Melancolie type', () => {
    const wrapper = mountBadge('Melancolie');
    expect(wrapper.find('.type-badge__label').text()).toBe('Mélancolie');
  });

  it('renders for Rupture type', () => {
    const wrapper = mountBadge('Rupture');
    expect(wrapper.find('.type-badge__label').text()).toBe('Rupture');
  });

  it('renders for Memoire type', () => {
    const wrapper = mountBadge('Memoire');
    expect(wrapper.find('.type-badge__label').text()).toBe('Mémoire');
  });

  it('renders for Silence type', () => {
    const wrapper = mountBadge('Silence');
    expect(wrapper.find('.type-badge__label').text()).toBe('Silence');
  });

  it('exposes an unknown contract instead of falling back to Neutral', () => {
    const wrapper = mountBadge('UnknownType');
    expect(wrapper.find('.type-badge').exists()).toBe(true);
    expect(wrapper.find('.type-badge').classes()).toContain('type-badge--invalid');
    expect(wrapper.find('.type-badge__label').text()).toBe('UnknownType');
  });

  it('displays the glyph', () => {
    const wrapper = mountBadge('Effroi');
    expect(wrapper.find('.type-badge__glyph').text()).toBe('✶');
  });

  it('always displays the label next to the glyph', () => {
    const wrapper = mountBadge('Effroi');
    expect(wrapper.find('.type-badge__glyph').text()).toBe('✶');
    expect(wrapper.find('.type-badge__label').text()).toBe('Effroi');
  });

  it('sets title attribute with type label', () => {
    const wrapper = mountBadge('Effroi');
    expect(wrapper.find('.type-badge').attributes('title')).toContain('Effroi');
  });

  it('sets CSS custom property for color', () => {
    const wrapper = mountBadge('Effroi');
    const style = wrapper.find('.type-badge').attributes('style');
    expect(style).toContain('--type-color');
  });

  it('shows correct glyph for Deni', () => {
    const wrapper = mountBadge('Deni');
    expect(wrapper.find('.type-badge__glyph').text()).toBe('◇');
  });

  it('shows correct glyph for Melancolie', () => {
    const wrapper = mountBadge('Melancolie');
    expect(wrapper.find('.type-badge__glyph').text()).toBe('❍');
  });

  it('shows correct glyph for Memoire', () => {
    const wrapper = mountBadge('Memoire');
    expect(wrapper.find('.type-badge__glyph').text()).toBe('◈');
  });

  it('shows correct glyph for Silence', () => {
    const wrapper = mountBadge('Silence');
    expect(wrapper.find('.type-badge__glyph').text()).toBe('○');
  });
});
