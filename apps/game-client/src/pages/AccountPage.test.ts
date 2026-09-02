// @vitest-environment jsdom
import { afterEach, describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';

import AccountPage from './AccountPage.vue';

describe('AccountPage', () => {
  afterEach(() => { document.body.innerHTML = ''; });

  it('presents security, sessions and readable privacy data', () => {
    const wrapper = mount(AccountPage, {
      attachTo: document.body,
      global: { stubs: { LivingWalls: true, RouterLink: true } },
    });

    expect(wrapper.text()).toContain('Google Authenticator');
    expect(wrapper.text()).toContain('une seule session possède l’autorité');
    expect(wrapper.text()).toContain('Un export lisible par un humain');
    expect(wrapper.text()).toContain('30 jours');
  });

  it('prepares a human-readable export view', async () => {
    const wrapper = mount(AccountPage, {
      attachTo: document.body,
      global: { stubs: { LivingWalls: true, RouterLink: true } },
    });

    await wrapper.find('.primary-action').trigger('click');
    expect(wrapper.text()).toContain('Export prêt');
    expect(wrapper.text()).toContain('personnages · progression');
  });

  it('requires explicit confirmation before recording account closure', async () => {
    const wrapper = mount(AccountPage, {
      attachTo: document.body,
      global: { stubs: { LivingWalls: true, RouterLink: true } },
    });

    await wrapper.find('.danger-action').trigger('click');
    expect(document.body.textContent).toContain('Demander la fermeture');

    const confirm = document.body.querySelectorAll<HTMLButtonElement>('.danger-action');
    confirm[confirm.length - 1].click();
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain('Demande enregistrée');
  });
});
