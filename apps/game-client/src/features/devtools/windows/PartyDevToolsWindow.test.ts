// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import PartyDevToolsWindow from './PartyDevToolsWindow.vue';

function mountWindow(disabled = false, isLoading = false) {
  return mount(PartyDevToolsWindow, { props: { disabled, isLoading } });
}

describe('PartyDevToolsWindow', () => {
  it('shows all 5 recruitable companions', () => {
    const wrapper = mountWindow();
    for (const name of ['Thomas', 'Mané', 'Mina', 'Elise', 'John']) {
      expect(wrapper.text()).toContain(name);
    }
  });

  it('emits addAlly with the default companion key', async () => {
    const wrapper = mountWindow();
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Recruter'));
    await btn!.trigger('click');
    expect(wrapper.emitted('addAlly')).toEqual([['npc.thomas']]);
  });

  it('emits addAlly with a different companion once selected', async () => {
    const wrapper = mountWindow();
    const cell = wrapper.findAll('.devtools-catalog-cell').find((c) => c.text().includes('John'));
    await cell!.trigger('click');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Recruter'));
    await btn!.trigger('click');
    expect(wrapper.emitted('addAlly')).toEqual([['npc.john']]);
  });

  it('emits removeAlly when the remove button is clicked', async () => {
    const wrapper = mountWindow();
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Retirer'));
    await btn!.trigger('click');
    expect(wrapper.emitted('removeAlly')).toHaveLength(1);
  });
});
