// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import CursesDevToolsWindow from './CursesDevToolsWindow.vue';

function mountWindow(disabled = false, isLoading = false) {
  return mount(CursesDevToolsWindow, { props: { disabled, isLoading } });
}

describe('CursesDevToolsWindow', () => {
  it('emits activateCurse with the trimmed key', async () => {
    const wrapper = mountWindow();
    await wrapper.find('input').setValue('  curse.old-wound  ');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Activer'));
    await btn!.trigger('click');
    expect(wrapper.emitted('activateCurse')).toEqual([['curse.old-wound']]);
  });

  it('disables the activate button when the input is empty', () => {
    const wrapper = mountWindow();
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Activer'));
    expect((btn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('emits clearCurses on confirm', async () => {
    const wrapper = mountWindow();
    const originalConfirm = globalThis.window.confirm;
    globalThis.window.confirm = vi.fn(() => true);

    const btn = wrapper.findAll('button').find((b) => b.text().includes('Effacer'));
    await btn!.trigger('click');

    expect(wrapper.emitted('clearCurses')).toHaveLength(1);
    globalThis.window.confirm = originalConfirm;
  });
});
