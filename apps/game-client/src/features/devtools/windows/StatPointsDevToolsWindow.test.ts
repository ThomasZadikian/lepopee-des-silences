// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import StatPointsDevToolsWindow from './StatPointsDevToolsWindow.vue';

describe('StatPointsDevToolsWindow', () => {
  it('emits awardStatPoints with the default amount', async () => {
    const wrapper = mount(StatPointsDevToolsWindow, { props: { disabled: false, isLoading: false } });
    await wrapper.find('button').trigger('click');
    expect(wrapper.emitted('awardStatPoints')).toEqual([[1]]);
  });

  it('clamps the amount to [1,20]', async () => {
    const wrapper = mount(StatPointsDevToolsWindow, { props: { disabled: false, isLoading: false } });
    await wrapper.find('input[type="number"]').setValue(50);
    await wrapper.find('button').trigger('click');
    expect(wrapper.emitted('awardStatPoints')).toEqual([[20]]);
  });

  it('disables the button when disabled prop is true', () => {
    const wrapper = mount(StatPointsDevToolsWindow, { props: { disabled: true, isLoading: false } });
    expect((wrapper.find('button').element as HTMLButtonElement).disabled).toBe(true);
  });
});
