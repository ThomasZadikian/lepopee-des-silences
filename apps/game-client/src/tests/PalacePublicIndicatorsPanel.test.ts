// @vitest-environment jsdom
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

import PalacePublicIndicatorsPanel from '../features/palace-indicators/PalacePublicIndicatorsPanel.vue';
import type { PalacePublicIndicatorDto } from '../features/runs/types/runTypes';

const baseIndicator: PalacePublicIndicatorDto = {
  key: 'palace.whispers',
  label: 'Murmures du Palais',
  description: 'Le Palais observe la traversée.',
  category: 'run',
  level: 'high',
  tone: 'mystery',
  source: 'run',
};

describe('PalacePublicIndicatorsPanel', () => {
  it('shows an empty state without indicators', () => {
    const wrapper = mount(PalacePublicIndicatorsPanel, { props: { indicators: [] } });

    expect(wrapper.text()).toContain('Aucun indicateur public du Palais disponible.');
  });

  it('shows an empty state when indicators are null', () => {
    const wrapper = mount(PalacePublicIndicatorsPanel, { props: { indicators: null } });

    expect(wrapper.text()).toContain('Aucun indicateur public du Palais disponible.');
  });

  it('displays public indicator fields when provided', () => {
    const wrapper = mount(PalacePublicIndicatorsPanel, { props: { indicators: [baseIndicator] } });

    expect(wrapper.text()).toContain('Murmures du Palais');
    expect(wrapper.text()).toContain('Le Palais observe la traversée.');
    expect(wrapper.text()).toContain('run');
    expect(wrapper.text()).toContain('high');
    expect(wrapper.text()).toContain('palace.whispers');
  });

  it('does not break when optional fields are absent', () => {
    const wrapper = mount(PalacePublicIndicatorsPanel, {
      props: { indicators: [{ key: 'palace.quiet', label: 'Silence du Palais' }] },
    });

    expect(wrapper.text()).toContain('Silence du Palais');
    expect(wrapper.text()).toContain('palace.quiet');
  });

  it('does not render forbidden internal fields when present on malformed input', () => {
    const indicator = {
      ...baseIndicator,
      sourceDecisionId: 'decision-secret',
      createdAtUtc: '2026-06-18T00:00:00Z',
      expiresAtUtc: '2026-06-19T00:00:00Z',
      weight: '0.72',
      probability: '0.42',
      coefficient: '1.20',
      matrix: 'markov-room-type-0.1.0',
      markov: 'hidden-state',
      rawScore: '99',
      adaptiveScore: '88',
      activeModifiers: 'modifier-secret',
    } as unknown as PalacePublicIndicatorDto;

    const wrapper = mount(PalacePublicIndicatorsPanel, { props: { indicators: [indicator] } });
    const text = wrapper.text();

    expect(text).not.toContain('decision-secret');
    expect(text).not.toContain('2026-06-18');
    expect(text).not.toContain('0.72');
    expect(text).not.toContain('0.42');
    expect(text).not.toContain('1.20');
    expect(text).not.toContain('markov-room-type');
    expect(text).not.toContain('hidden-state');
    expect(text).not.toContain('99');
    expect(text).not.toContain('88');
    expect(text).not.toContain('modifier-secret');
  });
});
