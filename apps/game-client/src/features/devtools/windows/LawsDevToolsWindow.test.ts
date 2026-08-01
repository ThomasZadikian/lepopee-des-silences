// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import LawsDevToolsWindow from './LawsDevToolsWindow.vue';

function mountWindow(disabled = false, isLoading = false) {
  return mount(LawsDevToolsWindow, { props: { disabled, isLoading } });
}

describe('LawsDevToolsWindow', () => {
  it('emits activateLaw with the trimmed key', async () => {
    const wrapper = mountWindow();
    await wrapper.find('input').setValue('  law-aegis-v1  ');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Activer'));
    await btn!.trigger('click');
    expect(wrapper.emitted('activateLaw')).toEqual([['law-aegis-v1']]);
  });

  it('disables the activate button when the input is empty', () => {
    const wrapper = mountWindow();
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Activer'));
    expect((btn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('emits clearLaws on confirm', async () => {
    const wrapper = mountWindow();
    const originalConfirm = globalThis.window.confirm;
    globalThis.window.confirm = vi.fn(() => true);

    const btn = wrapper.findAll('button').find((b) => b.text().includes('Effacer'));
    await btn!.trigger('click');

    expect(wrapper.emitted('clearLaws')).toHaveLength(1);
    globalThis.window.confirm = originalConfirm;
  });
});
