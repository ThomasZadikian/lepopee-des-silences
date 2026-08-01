// @vitest-environment jsdom
import { afterEach, describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';

import DevToolsMicroMenu from './DevToolsMicroMenu.vue';

function windowStub(name: string) {
  return { name, template: `<div class="stub-${name}" />` };
}

function mountMenu() {
  return mount(DevToolsMicroMenu, {
    attachTo: document.body,
    props: {
      disabled: false,
      isLoading: false,
      characters: [],
      allSkills: [],
      allItems: [],
      psyche: null,
    },
    global: {
      stubs: {
        SkillsDevToolsWindow: windowStub('sorts-window'),
        ItemsDevToolsWindow: windowStub('objets-window'),
        StatPointsDevToolsWindow: windowStub('points-window'),
        RunDevToolsWindow: windowStub('run-window'),
        PartyDevToolsWindow: windowStub('compagnons-window'),
        RoomDevToolsWindow: windowStub('salle-window'),
        LawsDevToolsWindow: windowStub('lois-window'),
        CursesDevToolsWindow: windowStub('malediction-window'),
        PsycheDevToolsWindow: windowStub('psyche-window'),
      },
    },
  });
}

describe('DevToolsMicroMenu', () => {
  afterEach(() => {
    document.body.innerHTML = '';
  });

  it('renders one button per devtools window', () => {
    const wrapper = mountMenu();
    expect(wrapper.findAll('.devtools-micro-menu__btn')).toHaveLength(9);
  });

  it('gives each button a tooltip label', () => {
    const wrapper = mountMenu();
    const titles = wrapper.findAll('.devtools-micro-menu__btn').map((btn) => btn.attributes('title'));
    expect(titles).toEqual([
      'Sorts', 'Objets', 'Points de compétence', 'Run', 'Compagnons',
      'Salle', 'Lois', 'Malédictions', 'Psyché',
    ]);
  });

  it('shows no window by default', () => {
    mountMenu();
    expect(document.querySelector('.pom-backdrop')).toBeNull();
  });

  it('opens the Sorts window as a modal overlay', async () => {
    const wrapper = mountMenu();
    await wrapper.findAll('.devtools-micro-menu__btn')[0]!.trigger('click');
    expect(document.querySelector('.stub-sorts-window')).not.toBeNull();
  });

  it('opens the Objets window as a modal overlay', async () => {
    const wrapper = mountMenu();
    await wrapper.findAll('.devtools-micro-menu__btn')[1]!.trigger('click');
    expect(document.querySelector('.stub-objets-window')).not.toBeNull();
  });

  it('marks the button for the currently open window as active', async () => {
    const wrapper = mountMenu();
    const buttons = wrapper.findAll('.devtools-micro-menu__btn');
    await buttons[0]!.trigger('click');
    expect(buttons[0]!.classes()).toContain('devtools-micro-menu__btn--active');
    expect(buttons[1]!.classes()).not.toContain('devtools-micro-menu__btn--active');
  });

  it('closes the window when PageOverlayModal emits close', async () => {
    const wrapper = mountMenu();
    await wrapper.findAll('.devtools-micro-menu__btn')[0]!.trigger('click');
    expect(document.querySelector('.pom-backdrop')).not.toBeNull();

    const closeBtn = document.querySelector('.pom-close') as HTMLButtonElement;
    closeBtn.click();
    await wrapper.vm.$nextTick();
    expect(document.querySelector('.pom-backdrop')).toBeNull();
  });
});
