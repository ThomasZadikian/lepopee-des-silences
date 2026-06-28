// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import RunHudPanel from './RunHudPanel.vue';
import type { RunDto } from '../types/runTypes';

function mountPanel(run: RunDto) {
  return mount(RunHudPanel, { props: { run } });
}

describe('RunHudPanel', () => {
  const baseRun: RunDto = {
    id: 'run-1',
    seed: 'abc123',
    generatorVersion: 'v1.0',
    markovMatrixVersion: 'v2.0',
    status: 'Active',
    currentRoomIndex: 2,
    currentRoomNumber: 3,
    currentRoom: {
      id: 'room-1',
      roomType: 'Combat',
      theme: 'Salle de combat',
      currentNodeDepth: 1,
      maxNodeDepth: 4,
      nodes: [],
      availableNodes: [],
      bossPreview: {
        name: 'Gardien de la Salle',
        dangerHint: 'High',
      },
      layoutTemplateKey: 'template-a',
      layoutTemplateVersion: 'v1',
    },
  };

  it('renders without crashing', () => {
    const wrapper = mountPanel(baseRun);
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the current room number', () => {
    const wrapper = mountPanel(baseRun);
    expect(wrapper.text()).toContain('3');
  });

  it('displays the room type', () => {
    const wrapper = mountPanel(baseRun);
    expect(wrapper.text()).toContain('Combat');
  });

  it('displays the progression', () => {
    const wrapper = mountPanel(baseRun);
    expect(wrapper.text()).toContain('2');
    expect(wrapper.text()).toContain('5');
  });

  it('displays the boss name', () => {
    const wrapper = mountPanel(baseRun);
    expect(wrapper.text()).toContain('Gardien de la Salle');
  });

  it('displays the boss danger hint', () => {
    const wrapper = mountPanel(baseRun);
    expect(wrapper.text()).toContain('High');
  });

  it('applies correct danger class for critical', () => {
    const run = {
      ...baseRun,
      currentRoom: { ...baseRun.currentRoom, bossPreview: { name: 'Boss', dangerHint: 'Critical' } },
    };
    const wrapper = mountPanel(run);
    expect(wrapper.find('.danger--critical').exists()).toBe(true);
  });

  it('applies correct danger class for high', () => {
    const wrapper = mountPanel(baseRun);
    expect(wrapper.find('.danger--high').exists()).toBe(true);
  });

  it('applies correct danger class for moderate', () => {
    const run = {
      ...baseRun,
      currentRoom: { ...baseRun.currentRoom, bossPreview: { name: 'Boss', dangerHint: 'Moderate' } },
    };
    const wrapper = mountPanel(run);
    expect(wrapper.find('.danger--moderate').exists()).toBe(true);
  });

  it('applies correct danger class for low', () => {
    const run = {
      ...baseRun,
      currentRoom: { ...baseRun.currentRoom, bossPreview: { name: 'Boss', dangerHint: 'Low' } },
    };
    const wrapper = mountPanel(run);
    expect(wrapper.find('.danger--low').exists()).toBe(true);
  });

  it('displays debug information', () => {
    const wrapper = mountPanel(baseRun);
    expect(wrapper.text()).toContain('abc123');
    expect(wrapper.text()).toContain('v1.0');
    expect(wrapper.text()).toContain('v2.0');
  });

  it('displays layout template when available', () => {
    const wrapper = mountPanel(baseRun);
    expect(wrapper.text()).toContain('template-a');
  });

  it('hides layout template row when not available', () => {
    const run = {
      ...baseRun,
      currentRoom: { ...baseRun.currentRoom, layoutTemplateKey: null },
    };
    const wrapper = mountPanel(run);
    expect(wrapper.text()).not.toContain('Template');
  });

  it('calculates progress bar width correctly', () => {
    const wrapper = mountPanel(baseRun);
    const bar = wrapper.find('.hud__progress-bar');
    // (1+1)/(4+1) = 40%
    expect(bar.attributes('style')).toContain('40%');
  });

  it('handles room index 0 correctly', () => {
    const run = { ...baseRun, currentRoomIndex: 0 };
    const wrapper = mountPanel(run);
    expect(wrapper.text()).toContain('1');
  });

  it('handles node depth 0 correctly', () => {
    const run = {
      ...baseRun,
      currentRoom: { ...baseRun.currentRoom, currentNodeDepth: 0 },
    };
    const wrapper = mountPanel(run);
    expect(wrapper.text()).toContain('1');
  });
});
