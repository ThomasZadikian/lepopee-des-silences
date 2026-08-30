// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';

import StatRadarChart from './StatRadarChart.vue';
import { statOrder, statLabels, statRadarMax } from '../../../party/constants/statDescriptions';
import type { PlayerStatKind } from '../../../party/types/playerTypes';

function zeroValues(): Record<PlayerStatKind, number> {
  const values = {} as Record<PlayerStatKind, number>;
  for (const stat of statOrder) values[stat] = 0;
  return values;
}

describe('StatRadarChart', () => {
  it('renders exactly one axis, marker, and label per stat', () => {
    const wrapper = mount(StatRadarChart, { props: { values: zeroValues() } });

    expect(wrapper.findAll('.stat-radar__axis')).toHaveLength(statOrder.length);
    expect(wrapper.findAll('.stat-radar__marker')).toHaveLength(statOrder.length);
    expect(wrapper.findAll('.stat-radar__label')).toHaveLength(statOrder.length);
  });

  it('labels the axes in statOrder order', () => {
    const wrapper = mount(StatRadarChart, { props: { values: zeroValues() } });

    const labels = wrapper.findAll('.stat-radar__label').map((el) => el.text());
    expect(labels).toEqual(statOrder.map((stat) => statLabels[stat]));
  });

  it('collapses all markers to the center when every stat is zero', () => {
    const wrapper = mount(StatRadarChart, { props: { values: zeroValues() } });

    const markers = wrapper.findAll('.stat-radar__marker');
    for (const marker of markers) {
      expect(marker.attributes('cx')).toBe('160');
      expect(marker.attributes('cy')).toBe('160');
    }
  });

  it('places a maxed-out stat at the top axis, at the full chart radius', () => {
    const values = zeroValues();
    const firstStat = statOrder[0];
    values[firstStat] = statRadarMax[firstStat];

    const wrapper = mount(StatRadarChart, { props: { values } });

    const marker = wrapper.findAll('.stat-radar__marker')[0];
    // First axis points straight up (angle = -90deg): x == center, y == center - maxRadius.
    expect(Number(marker.attributes('cx'))).toBeCloseTo(160, 0);
    expect(Number(marker.attributes('cy'))).toBeCloseTo(40, 0);
  });

  it('does not render a preview polygon when no preview is given', () => {
    const wrapper = mount(StatRadarChart, { props: { values: zeroValues() } });

    expect(wrapper.find('.stat-radar__polygon--preview').exists()).toBe(false);
  });

  it('does not render a preview polygon when preview equals the current values', () => {
    const values = zeroValues();
    const wrapper = mount(StatRadarChart, {
      props: { values, previewValues: { ...values } },
    });

    expect(wrapper.find('.stat-radar__polygon--preview').exists()).toBe(false);
  });

  it('renders a preview polygon when preview differs from the current values', () => {
    const values = zeroValues();
    const preview = { ...values, [statOrder[0]]: statRadarMax[statOrder[0]] };
    const wrapper = mount(StatRadarChart, { props: { values, previewValues: preview } });

    expect(wrapper.find('.stat-radar__polygon--preview').exists()).toBe(true);
  });
});
