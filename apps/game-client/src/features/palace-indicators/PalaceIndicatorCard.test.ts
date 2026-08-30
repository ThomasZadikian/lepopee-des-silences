// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import PalaceIndicatorCard from './PalaceIndicatorCard.vue';
import type { PalacePublicIndicatorDto } from '../runs/types/runTypes';

function mountCard(indicator: PalacePublicIndicatorDto) {
  return mount(PalaceIndicatorCard, { props: { indicator } });
}

describe('PalaceIndicatorCard', () => {
  const baseIndicator: PalacePublicIndicatorDto = {
    key: 'indicator-1',
    label: 'Tension ambiante',
    description: 'Le Palais est en état de tension.',
    tone: 'danger',
    level: 'High',
    category: 'Atmosphere',
    source: 'law-tension-v1',
  };

  it('renders without crashing', () => {
    const wrapper = mountCard(baseIndicator);
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the label', () => {
    const wrapper = mountCard(baseIndicator);
    expect(wrapper.text()).toContain('Tension ambiante');
  });

  it('displays the description', () => {
    const wrapper = mountCard(baseIndicator);
    expect(wrapper.text()).toContain('Le Palais est en état de tension.');
  });

  it('displays the level chip', () => {
    const wrapper = mountCard(baseIndicator);
    expect(wrapper.text()).toContain('High');
  });

  it('displays the tone chip when no level', () => {
    const indicator = { ...baseIndicator, level: undefined };
    const wrapper = mountCard(indicator);
    expect(wrapper.text()).toContain('danger');
  });

  it('displays the key', () => {
    const wrapper = mountCard(baseIndicator);
    expect(wrapper.text()).toContain('indicator-1');
  });

  it('displays the category', () => {
    const wrapper = mountCard(baseIndicator);
    expect(wrapper.text()).toContain('Atmosphere');
  });

  it('displays the source', () => {
    const wrapper = mountCard(baseIndicator);
    expect(wrapper.text()).toContain('law-tension-v1');
  });

  it('applies danger tone class', () => {
    const wrapper = mountCard(baseIndicator);
    expect(wrapper.find('.pic-root--danger').exists()).toBe(true);
  });

  it('applies unstable tone class', () => {
    const indicator = { ...baseIndicator, tone: 'unstable' };
    const wrapper = mountCard(indicator);
    expect(wrapper.find('.pic-root--unstable').exists()).toBe(true);
  });

  it('applies mystery tone class', () => {
    const indicator = { ...baseIndicator, tone: 'mystery' };
    const wrapper = mountCard(indicator);
    expect(wrapper.find('.pic-root--mystery').exists()).toBe(true);
  });

  it('applies calm tone class', () => {
    const indicator = { ...baseIndicator, tone: 'calm' };
    const wrapper = mountCard(indicator);
    expect(wrapper.find('.pic-root--calm').exists()).toBe(true);
  });

  it('uses key as fallback when label is missing', () => {
    const indicator = { ...baseIndicator, label: '' };
    const wrapper = mountCard(indicator);
    expect(wrapper.text()).toContain('indicator-1');
  });

  it('hides description when not provided', () => {
    const indicator = { ...baseIndicator, description: undefined };
    const wrapper = mountCard(indicator);
    expect(wrapper.find('.pic-description').exists()).toBe(false);
  });

  it('hides category when not provided', () => {
    const indicator = { ...baseIndicator, category: undefined };
    const wrapper = mountCard(indicator);
    expect(wrapper.text()).not.toContain('Catégorie');
  });

  it('hides source when not provided', () => {
    const indicator = { ...baseIndicator, source: undefined };
    const wrapper = mountCard(indicator);
    expect(wrapper.text()).not.toContain('Source');
  });

  it('handles unknown tone gracefully', () => {
    const indicator = { ...baseIndicator, tone: 'unknown' };
    const wrapper = mountCard(indicator);
    expect(wrapper.exists()).toBe(true);
    expect(wrapper.find('.pic-root--danger').exists()).toBe(false);
  });

  it('handles null tone gracefully', () => {
    const indicator = { ...baseIndicator, tone: null };
    const wrapper = mountCard(indicator);
    expect(wrapper.exists()).toBe(true);
  });
});
