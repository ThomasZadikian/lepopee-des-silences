// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

import AccountAccessPage from './AccountAccessPage.vue';

const router = { push: vi.fn() };

vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>();
  return { ...actual, useRouter: () => router };
});

function mountMode(mode: InstanceType<typeof AccountAccessPage>['$props']['mode']) {
  return mount(AccountAccessPage, {
    props: { mode },
    global: { stubs: { LivingWalls: true, RouterLink: true } },
  });
}

describe('AccountAccessPage', () => {
  beforeEach(() => vi.clearAllMocks());

  it.each([
    ['login', 'Revenir au Palais'],
    ['register', 'Entrer dans le Palais'],
    ['verify-email', 'Confirmer votre adresse'],
    ['mfa-setup', 'Lier Google Authenticator'],
    ['mfa-challenge', 'Prouver votre présence'],
    ['password-recovery', 'Retrouver votre accès'],
    ['password-reset', 'Choisir un nouveau mot de passe'],
  ] as const)('renders the %s screen', (mode, title) => {
    const wrapper = mountMode(mode);
    expect(wrapper.text()).toContain(title);
  });

  it('validates registration locally before entering the server-backed onboarding', async () => {
    const wrapper = mountMode('register');
    await wrapper.find('form').trigger('submit');
    expect(wrapper.text()).toContain('Une adresse e-mail est requise');

    const inputs = wrapper.findAll('input');
    await inputs[0].setValue('player@example.fr');
    await inputs[1].setValue('Nocturne');
    await inputs[2].setValue('correcthorse');
    await inputs[3].setValue('correcthorse');
    await inputs[4].setValue(true);
    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(router.push).toHaveBeenCalledWith({ name: 'verify-email' });
  });

  it('routes an interactive login to the mandatory MFA challenge', async () => {
    const wrapper = mountMode('login');
    const inputs = wrapper.findAll('input');
    await inputs[0].setValue('player@example.fr');
    await inputs[1].setValue('correcthorse');
    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(router.push).toHaveBeenCalledWith({ name: 'mfa-challenge' });
  });

  it('rejects an invalid TOTP and accepts a six digit challenge', async () => {
    const wrapper = mountMode('mfa-challenge');
    const input = wrapper.find('input');
    await input.setValue('12');
    await wrapper.find('form').trigger('submit');
    expect(wrapper.text()).toContain('six chiffres');

    await input.setValue('123456');
    await wrapper.find('form').trigger('submit');
    await flushPromises();
    expect(router.push).toHaveBeenCalledWith({ name: 'character-selection' });
  });

  it('keeps password recovery response enumeration-safe', async () => {
    const wrapper = mountMode('password-recovery');
    await wrapper.find('input').setValue('unknown@example.fr');
    await wrapper.find('form').trigger('submit');

    expect(wrapper.text()).toContain('Si cette adresse correspond à un compte');
    expect(router.push).not.toHaveBeenCalled();
  });

  it('validates password confirmation before reset', async () => {
    const wrapper = mountMode('password-reset');
    const inputs = wrapper.findAll('input');
    await inputs[0].setValue('correcthorse');
    await inputs[1].setValue('differentpass');
    await wrapper.find('form').trigger('submit');
    expect(wrapper.text()).toContain('ne correspondent pas');

    await inputs[1].setValue('correcthorse');
    await wrapper.find('form').trigger('submit');
    await flushPromises();
    expect(router.push).toHaveBeenCalledWith({ name: 'login' });
  });
});
