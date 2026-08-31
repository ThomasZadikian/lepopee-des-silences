import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';
import DevToolsMicroMenu from './DevToolsMicroMenu.vue';

const baseProps = {
  disabled: false,
  isLoading: false,
  characters: [],
  allSkills: [],
  allItems: [],
  allLaws: [],
  allCurses: [],
  psyche: null,
};

const stubs = {
  PageOverlayModal: {
    template: '<div class="modal"><slot/><button class="close" @click="$emit(\'close\')">close</button></div>',
  },
  SkillsDevToolsWindow: {
    template: '<button data-child="sorts" @click="$emit(\'unlockSkill\', \'char-1\', \'skill-1\')">sorts</button>',
  },
  ItemsDevToolsWindow: {
    template: '<button data-child="objets" @click="$emit(\'addItem\', \'item-1\', 2)">objets</button>',
  },
  RunDevToolsWindow: {
    template: '<div data-child="run"><button class="one" @click="$emit(\'advanceRoom\')"/><button class="many" @click="$emit(\'advanceRooms\', 3)"/></div>',
  },
  PartyDevToolsWindow: {
    template: '<div data-child="compagnons"><button class="add" @click="$emit(\'addAlly\', \'npc-1\')"/><button class="remove" @click="$emit(\'removeAlly\')"/></div>',
  },
  RoomDevToolsWindow: {
    template: '<div data-child="salle"><button class="state" @click="$emit(\'forcePalaceState\', \'Freed\')"/><button class="climate" @click="$emit(\'forceClimate\', \'Calm\')"/></div>',
  },
  LawsDevToolsWindow: {
    template: '<div data-child="lois"><button class="add" @click="$emit(\'activateLaw\', \'law-1\')"/><button class="clear" @click="$emit(\'clearLaws\')"/></div>',
  },
  CursesDevToolsWindow: {
    template: '<div data-child="malediction"><button class="add" @click="$emit(\'activateCurse\', \'curse-1\')"/><button class="clear" @click="$emit(\'clearCurses\')"/></div>',
  },
  PsycheDevToolsWindow: {
    template: '<button data-child="psyche" @click="$emit(\'refresh\')">psyche</button>',
  },
};

describe('DevToolsMicroMenu coverage margin', () => {
  it('opens every window, forwards every action and closes the overlay', async () => {
    const host = document.createElement('div');
    document.body.appendChild(host);
    const wrapper = mount(DevToolsMicroMenu, {
      attachTo: host,
      props: baseProps,
      global: { stubs },
    });

    const buttons = wrapper.findAll('.devtools-micro-menu__btn');
    expect(buttons).toHaveLength(8);

    await buttons[0]!.trigger('click');
    await document.querySelector<HTMLElement>('[data-child="sorts"]')!.click();
    expect(wrapper.emitted('unlockSkill')).toEqual([['char-1', 'skill-1']]);

    await buttons[1]!.trigger('click');
    document.querySelector<HTMLElement>('[data-child="objets"]')!.click();
    expect(wrapper.emitted('addItem')).toEqual([['item-1', 2]]);

    await buttons[2]!.trigger('click');
    document.querySelector<HTMLElement>('[data-child="run"] .one')!.click();
    document.querySelector<HTMLElement>('[data-child="run"] .many')!.click();
    expect(wrapper.emitted('advanceRoom')).toHaveLength(1);
    expect(wrapper.emitted('advanceRooms')).toEqual([[3]]);

    await buttons[3]!.trigger('click');
    document.querySelector<HTMLElement>('[data-child="compagnons"] .add')!.click();
    document.querySelector<HTMLElement>('[data-child="compagnons"] .remove')!.click();
    expect(wrapper.emitted('addAlly')).toEqual([['npc-1']]);
    expect(wrapper.emitted('removeAlly')).toHaveLength(1);

    await buttons[4]!.trigger('click');
    document.querySelector<HTMLElement>('[data-child="salle"] .state')!.click();
    document.querySelector<HTMLElement>('[data-child="salle"] .climate')!.click();
    expect(wrapper.emitted('forcePalaceState')).toEqual([['Freed']]);
    expect(wrapper.emitted('forceClimate')).toEqual([['Calm']]);

    await buttons[5]!.trigger('click');
    document.querySelector<HTMLElement>('[data-child="lois"] .add')!.click();
    document.querySelector<HTMLElement>('[data-child="lois"] .clear')!.click();
    expect(wrapper.emitted('activateLaw')).toEqual([['law-1']]);
    expect(wrapper.emitted('clearLaws')).toHaveLength(1);

    await buttons[6]!.trigger('click');
    document.querySelector<HTMLElement>('[data-child="malediction"] .add')!.click();
    document.querySelector<HTMLElement>('[data-child="malediction"] .clear')!.click();
    expect(wrapper.emitted('activateCurse')).toEqual([['curse-1']]);
    expect(wrapper.emitted('clearCurses')).toHaveLength(1);

    await buttons[7]!.trigger('click');
    document.querySelector<HTMLElement>('[data-child="psyche"]')!.click();
    expect(wrapper.emitted('refreshPsyche')).toHaveLength(1);

    expect(document.querySelector('.modal')).not.toBeNull();
    document.querySelector<HTMLElement>('.modal .close')!.click();
    await wrapper.vm.$nextTick();
    expect(document.querySelector('.modal')).toBeNull();

    wrapper.unmount();
    host.remove();
  });
});
