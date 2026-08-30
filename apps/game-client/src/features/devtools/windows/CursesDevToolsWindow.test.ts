// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import CursesDevToolsWindow from './CursesDevToolsWindow.vue';
import type { CurseDefinitionView } from '../../palace-laws/types/curseTypes';

const allCurses: CurseDefinitionView[] = [
  {
    key: 'curse.old-wound', displayName: 'Vieille blessure', description: 'Rouvre une plaie ancienne.',
    narrativeText: null, severity: 3, duration: 'Permanent', trigger: null,
  },
  {
    key: 'curse.silence', displayName: 'Silence pesant', description: 'Étouffe les cris.',
    narrativeText: 'Personne ne vous entendra.', severity: 1, duration: 'Room', trigger: 'RoomEnter',
  },
];

function mountWindow(disabled = false, isLoading = false) {
  return mount(CursesDevToolsWindow, { props: { disabled, isLoading, allCurses } });
}

describe('CursesDevToolsWindow', () => {
  it('lists every curse in the catalog grid', () => {
    const wrapper = mountWindow();
    expect(wrapper.text()).toContain('Vieille blessure');
    expect(wrapper.text()).toContain('Silence pesant');
  });

  it('filters curses by search query', async () => {
    const wrapper = mountWindow();
    await wrapper.find('input.devtools-input').setValue('silence');
    expect(wrapper.text()).toContain('Silence pesant');
    expect(wrapper.text()).not.toContain('Vieille blessure');
  });

  it('shows the description sheet only once a curse is selected', async () => {
    const wrapper = mountWindow();
    expect(wrapper.text()).not.toContain('Rouvre une plaie ancienne.');
    const cell = wrapper.findAll('.devtools-catalog-cell').find((c) => c.text().includes('Vieille blessure'));
    await cell!.trigger('click');
    expect(wrapper.text()).toContain('Rouvre une plaie ancienne.');
  });

  it('emits activateCurse with the selected key', async () => {
    const wrapper = mountWindow();
    const cell = wrapper.findAll('.devtools-catalog-cell').find((c) => c.text().includes('Vieille blessure'));
    await cell!.trigger('click');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Activer cette malédiction'));
    await btn!.trigger('click');
    expect(wrapper.emitted('activateCurse')).toEqual([['curse.old-wound']]);
  });

  it('shows nothing to activate when no curse is selected', () => {
    const wrapper = mountWindow();
    expect(wrapper.findAll('button').find((b) => b.text().includes('Activer cette malédiction'))).toBeUndefined();
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
