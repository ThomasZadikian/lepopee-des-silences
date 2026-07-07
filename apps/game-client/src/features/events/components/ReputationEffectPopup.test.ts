// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import ReputationEffectPopup from './ReputationEffectPopup.vue';

function mountPopup(effects: { id: number; amount: number; npcName: string }[]) {
  return mount(ReputationEffectPopup, {
    props: { effects },
    global: { stubs: { TransitionGroup: { template: '<div><slot /></div>' } } },
  });
}

describe('ReputationEffectPopup', () => {
  it('renders a pill per effect', () => {
    const wrapper = mountPopup([
      { id: 1, amount: 2, npcName: 'Hitomi' },
      { id: 2, amount: -3, npcName: 'Le Majordome' },
    ]);
    expect(wrapper.findAll('.rep-pill')).toHaveLength(2);
  });

  it('shows a plus sign and gain styling for positive amounts', () => {
    const wrapper = mountPopup([{ id: 1, amount: 2, npcName: 'Hitomi' }]);
    expect(wrapper.text()).toContain('+2');
    expect(wrapper.find('.rep-pill--gain').exists()).toBe(true);
  });

  it('shows the raw negative amount and loss styling for negative amounts', () => {
    const wrapper = mountPopup([{ id: 1, amount: -3, npcName: 'Le Majordome' }]);
    expect(wrapper.text()).toContain('-3');
    expect(wrapper.find('.rep-pill--loss').exists()).toBe(true);
  });

  it('displays the npc name label', () => {
    const wrapper = mountPopup([{ id: 1, amount: 1, npcName: 'Hitomi' }]);
    expect(wrapper.text()).toContain('Hitomi');
  });

  it('emits dismiss for a new effect after the auto-dismiss delay', () => {
    vi.useFakeTimers();
    const wrapper = mountPopup([{ id: 1, amount: 1, npcName: 'Hitomi' }]);

    vi.advanceTimersByTime(1800);

    expect(wrapper.emitted('dismiss')).toEqual([[1]]);
    vi.useRealTimers();
  });

  it('does not re-schedule dismissal for effects already present', async () => {
    vi.useFakeTimers();
    const wrapper = mountPopup([{ id: 1, amount: 1, npcName: 'Hitomi' }]);
    vi.advanceTimersByTime(1800);
    expect(wrapper.emitted('dismiss')).toHaveLength(1);

    await wrapper.setProps({ effects: [{ id: 1, amount: 1, npcName: 'Hitomi' }] });
    vi.advanceTimersByTime(1800);

    expect(wrapper.emitted('dismiss')).toHaveLength(1);
    vi.useRealTimers();
  });
});
