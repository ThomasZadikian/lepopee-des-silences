// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';

import TeamMicroMenu from './TeamMicroMenu.vue';

const routerLinkStub = {
  template: '<a :href="to"><slot /></a>',
  props: ['to'],
};

describe('TeamMicroMenu', () => {
  it('renders one button per destination', () => {
    const wrapper = mount(TeamMicroMenu, {
      global: { stubs: { RouterLink: routerLinkStub } },
    });

    const buttons = wrapper.findAll('.micro-menu__btn');
    expect(buttons).toHaveLength(4);
  });

  it('links to the four independent pages', () => {
    const wrapper = mount(TeamMicroMenu, {
      global: { stubs: { RouterLink: routerLinkStub } },
    });

    const hrefs = wrapper.findAll('.micro-menu__btn').map((btn) => btn.attributes('href'));
    expect(hrefs).toEqual(['/equipe', '/statistiques', '/grimoire', '/equipement']);
  });

  it('gives each button a tooltip label', () => {
    const wrapper = mount(TeamMicroMenu, {
      global: { stubs: { RouterLink: routerLinkStub } },
    });

    const titles = wrapper.findAll('.micro-menu__btn').map((btn) => btn.attributes('title'));
    expect(titles).toEqual(['Équipe', 'Statistiques', 'Grimoire', 'Équipement']);
  });
});
