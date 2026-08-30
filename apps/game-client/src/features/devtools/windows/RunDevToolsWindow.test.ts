// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import RunDevToolsWindow from './RunDevToolsWindow.vue';

function mountWindow(disabled = false, isLoading = false) {
  return mount(RunDevToolsWindow, { props: { disabled, isLoading } });
}

describe('RunDevToolsWindow', () => {
  it('emits advanceRoom when button is clicked', async () => {
    const wrapper = mountWindow();
    const btn = wrapper.findAll('button').find((b) => b.text().includes("d'une salle"));
    await btn!.trigger('click');
    expect(wrapper.emitted('advanceRoom')).toHaveLength(1);
  });

  it('emits advanceRooms with the clamped count', async () => {
    const wrapper = mountWindow();
    await wrapper.find('input[type="number"]').setValue(20);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('salle(s)'));
    await btn!.trigger('click');
    expect(wrapper.emitted('advanceRooms')).toEqual([[10]]);
  });

  it('disables all buttons when disabled prop is true', () => {
    const wrapper = mountWindow(true);
    for (const btn of wrapper.findAll('button')) {
      expect((btn.element as HTMLButtonElement).disabled).toBe(true);
    }
  });
});
