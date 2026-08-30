// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import RoomDevToolsWindow from './RoomDevToolsWindow.vue';

function mountWindow(disabled = false, isLoading = false) {
  return mount(RoomDevToolsWindow, { props: { disabled, isLoading } });
}

describe('RoomDevToolsWindow', () => {
  it('emits forcePalaceState with the selected state', async () => {
    const wrapper = mountWindow();
    await wrapper.findAll('select')[0]!.setValue('Enraged');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Forcer cet état'));
    await btn!.trigger('click');
    expect(wrapper.emitted('forcePalaceState')).toEqual([['Enraged']]);
  });

  it('emits forceClimate with the selected climate', async () => {
    const wrapper = mountWindow();
    await wrapper.findAll('select')[1]!.setValue('Rain');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Forcer ce climat'));
    await btn!.trigger('click');
    expect(wrapper.emitted('forceClimate')).toEqual([['Rain']]);
  });

  it('shows all palace states and climates in their selectors', () => {
    const wrapper = mountWindow();
    expect(wrapper.findAll('select')[0]!.findAll('option')).toHaveLength(5);
    expect(wrapper.findAll('select')[1]!.findAll('option')).toHaveLength(5);
  });
});
