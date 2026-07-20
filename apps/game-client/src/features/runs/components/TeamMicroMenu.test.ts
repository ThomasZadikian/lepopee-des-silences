// @vitest-environment jsdom
import { afterEach, describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';

import TeamMicroMenu from './TeamMicroMenu.vue';

function pageStub(name: string) {
  return { name, props: ['embedded'], template: `<div class="stub-${name}" />` };
}

function mountMenu() {
  return mount(TeamMicroMenu, {
    attachTo: document.body,
    global: {
      stubs: {
        TeamPage: pageStub('team-page'),
        StatsPage: pageStub('stats-page'),
        GrimoirePage: pageStub('grimoire-page'),
        EquipmentPage: pageStub('equipment-page'),
      },
    },
  });
}

describe('TeamMicroMenu', () => {
  afterEach(() => {
    document.body.innerHTML = '';
  });

  it('renders one button per destination', () => {
    const wrapper = mountMenu();
    const buttons = wrapper.findAll('.micro-menu__btn');
    expect(buttons).toHaveLength(4);
  });

  it('gives each button a tooltip label', () => {
    const wrapper = mountMenu();
    const titles = wrapper.findAll('.micro-menu__btn').map((btn) => btn.attributes('title'));
    expect(titles).toEqual(['Équipe', 'Statistiques', 'Grimoire', 'Équipement']);
  });

  it('shows no modal by default', () => {
    mountMenu();
    expect(document.querySelector('.pom-backdrop')).toBeNull();
  });

  it('opens the Équipe page as a modal overlay instead of navigating', async () => {
    const wrapper = mountMenu();
    await wrapper.findAll('.micro-menu__btn')[0].trigger('click');
    expect(document.querySelector('.stub-team-page')).not.toBeNull();
  });

  it('opens the Statistiques page as a modal overlay', async () => {
    const wrapper = mountMenu();
    await wrapper.findAll('.micro-menu__btn')[1].trigger('click');
    expect(document.querySelector('.stub-stats-page')).not.toBeNull();
  });

  it('opens the Grimoire page as a modal overlay', async () => {
    const wrapper = mountMenu();
    await wrapper.findAll('.micro-menu__btn')[2].trigger('click');
    expect(document.querySelector('.stub-grimoire-page')).not.toBeNull();
  });

  it('opens the Équipement page as a modal overlay', async () => {
    const wrapper = mountMenu();
    await wrapper.findAll('.micro-menu__btn')[3].trigger('click');
    expect(document.querySelector('.stub-equipment-page')).not.toBeNull();
  });

  it('marks the button for the currently open modal as active', async () => {
    const wrapper = mountMenu();
    const buttons = wrapper.findAll('.micro-menu__btn');
    await buttons[0].trigger('click');
    expect(buttons[0].classes()).toContain('micro-menu__btn--active');
    expect(buttons[1].classes()).not.toContain('micro-menu__btn--active');
  });

  it('closes the modal when PageOverlayModal emits close', async () => {
    const wrapper = mountMenu();
    await wrapper.findAll('.micro-menu__btn')[0].trigger('click');
    expect(document.querySelector('.pom-backdrop')).not.toBeNull();

    const closeBtn = document.querySelector('.pom-close') as HTMLButtonElement;
    closeBtn.click();
    await wrapper.vm.$nextTick();
    expect(document.querySelector('.pom-backdrop')).toBeNull();
  });
});
