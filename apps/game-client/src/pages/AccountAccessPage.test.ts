// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

import AccountAccessPage from './AccountAccessPage.vue';

const router = { push: vi.fn() };
const route = { query: {} as Record<string, string> };
const api = vi.hoisted(() => ({
  registerAccount: vi.fn(),
  verifyEmail: vi.fn(),
  beginLogin: vi.fn(),
  beginMfaEnrollment: vi.fn(),
  confirmMfaEnrollment: vi.fn(),
  completeMfaChallenge: vi.fn(),
  requestPasswordReset: vi.fn(),
  resetPassword: vi.fn(),
}));
const auth = vi.hoisted(() => ({
  setChallengeToken: vi.fn(),
  getChallengeToken: vi.fn(() => 'challenge-token'),
  setAuthenticatedSession: vi.fn(),
}));

vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>();
  return { ...actual, useRouter: () => router, useRoute: () => route };
});
vi.mock('../shared/api/playerApi', () => ({ playerApi: api }));
vi.mock('../features/account/authSession', () => auth);

function mountMode(mode: InstanceType<typeof AccountAccessPage>['$props']['mode']) {
  return mount(AccountAccessPage, {
    props: { mode },
    global: { stubs: { LivingWalls: true, RouterLink: true } },
  });
}

describe('AccountAccessPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    route.query = {};
    auth.getChallengeToken.mockReturnValue('challenge-token');
    api.registerAccount.mockResolvedValue({ accountId: 'account-id', email: 'player@example.fr', emailVerificationRequired: true });
    api.verifyEmail.mockResolvedValue({ accountId: 'account-id', verified: true });
    api.beginLogin.mockResolvedValue({ status: 'mfa-required', challengeToken: 'challenge-token', emailVerificationRequired: false });
    api.beginMfaEnrollment.mockResolvedValue({ challengeToken: 'challenge-token', otpAuthUri: 'otpauth://totp/leds', manualEntryKey: 'MANUAL-KEY' });
    api.confirmMfaEnrollment.mockResolvedValue({ accountId: 'account-id', sessionId: 'session-id', accessToken: 'access-token', accessTokenExpiresAtUtc: '2026-08-31T13:00:00Z', recoveryCodes: ['RECOVERY'] });
    api.completeMfaChallenge.mockResolvedValue({ accountId: 'account-id', sessionId: 'session-id', accessToken: 'access-token', accessTokenExpiresAtUtc: '2026-08-31T13:00:00Z', recoveryCodes: null });
    api.requestPasswordReset.mockResolvedValue(undefined);
    api.resetPassword.mockResolvedValue(undefined);
  });

  it.each([
    ['login', 'Revenir au Palais'],
    ['register', 'Entrer dans le Palais'],
    ['verify-email', 'Confirmer votre adresse'],
    ['mfa-setup', 'Lier votre application TOTP'],
    ['mfa-challenge', 'Prouver votre présence'],
    ['password-recovery', 'Retrouver votre accès'],
    ['password-reset', 'Choisir un nouveau mot de passe'],
  ] as const)('renders the %s screen', (mode, title) => {
    const wrapper = mountMode(mode);
    expect(wrapper.text()).toContain(title);
  });

  it('creates the account through Player API before opening email verification', async () => {
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

    expect(api.registerAccount).toHaveBeenCalledWith({
      displayName: 'Nocturne',
      email: 'player@example.fr',
      password: 'correcthorse',
      ageConfirmed: true,
    });
    expect(router.push).toHaveBeenCalledWith({ name: 'verify-email' });
  });

  it('verifies the emailed token and returns to login for the MFA bootstrap', async () => {
    route.query = { token: 'email-token' };
    const wrapper = mountMode('verify-email');

    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(api.verifyEmail).toHaveBeenCalledWith('email-token');
    expect(router.push).toHaveBeenCalledWith({ name: 'login', query: { verified: '1' } });
  });

  it('routes login according to the server-issued MFA challenge', async () => {
    const wrapper = mountMode('login');
    const inputs = wrapper.findAll('input');
    await inputs[0].setValue('player@example.fr');
    await inputs[1].setValue('correcthorse');
    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(api.beginLogin).toHaveBeenCalledWith('player@example.fr', 'correcthorse');
    expect(auth.setChallengeToken).toHaveBeenCalledWith('challenge-token');
    expect(router.push).toHaveBeenCalledWith({ name: 'mfa-challenge' });

    api.beginLogin.mockResolvedValueOnce({ status: 'mfa-setup-required', challengeToken: 'setup-token' });
    await wrapper.find('form').trigger('submit');
    await flushPromises();
    expect(auth.setChallengeToken).toHaveBeenCalledWith('setup-token');
    expect(router.push).toHaveBeenLastCalledWith({ name: 'mfa-setup' });
  });

  it('loads MFA enrollment data and establishes the authenticated session', async () => {
    const wrapper = mountMode('mfa-setup');
    await flushPromises();
    expect(api.beginMfaEnrollment).toHaveBeenCalledWith('challenge-token');
    expect(wrapper.text()).toContain('MANUAL-KEY');
    expect(wrapper.get('img[alt="QR code de configuration de la double authentification"]').attributes('src'))
      .toMatch(/^data:image\/svg\+xml;charset=utf-8,/);

    await wrapper.find('input').setValue('123456');
    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(api.confirmMfaEnrollment).toHaveBeenCalledWith('challenge-token', '123456');
    expect(auth.setAuthenticatedSession).toHaveBeenCalled();
    expect(router.push).toHaveBeenCalledWith({ name: 'character-selection' });
  });

  it('rejects an invalid TOTP and completes a six digit challenge', async () => {
    const wrapper = mountMode('mfa-challenge');
    const input = wrapper.find('input');
    await input.setValue('12');
    await wrapper.find('form').trigger('submit');
    expect(wrapper.text()).toContain('six chiffres');

    await input.setValue('123456');
    await wrapper.find('form').trigger('submit');
    await flushPromises();
    expect(api.completeMfaChallenge).toHaveBeenCalledWith('challenge-token', '123456');
    expect(auth.setAuthenticatedSession).toHaveBeenCalled();
    expect(router.push).toHaveBeenCalledWith({ name: 'character-selection' });
  });

  it('keeps password recovery response enumeration-safe while calling the server', async () => {
    const wrapper = mountMode('password-recovery');
    await wrapper.find('input').setValue('unknown@example.fr');
    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(api.requestPasswordReset).toHaveBeenCalledWith('unknown@example.fr');
    expect(wrapper.text()).toContain('Si cette adresse correspond à un compte');
    expect(router.push).not.toHaveBeenCalled();
  });

  it('uses the emailed reset token after validating password confirmation', async () => {
    route.query = { token: 'reset-token' };
    const wrapper = mountMode('password-reset');
    const inputs = wrapper.findAll('input');
    await inputs[0].setValue('correcthorse');
    await inputs[1].setValue('differentpass');
    await wrapper.find('form').trigger('submit');
    expect(wrapper.text()).toContain('ne correspondent pas');

    await inputs[1].setValue('correcthorse');
    await wrapper.find('form').trigger('submit');
    await flushPromises();
    expect(api.resetPassword).toHaveBeenCalledWith('reset-token', 'correcthorse');
    expect(router.push).toHaveBeenCalledWith({ name: 'login' });
  });

  it('surfaces server errors without navigating', async () => {
    api.registerAccount.mockRejectedValueOnce(new Error('Adresse déjà utilisée'));
    const wrapper = mountMode('register');
    const inputs = wrapper.findAll('input');
    await inputs[0].setValue('player@example.fr');
    await inputs[1].setValue('Nocturne');
    await inputs[2].setValue('correcthorse');
    await inputs[3].setValue('correcthorse');
    await inputs[4].setValue(true);

    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(wrapper.text()).toContain('Adresse déjà utilisée');
    expect(router.push).not.toHaveBeenCalled();
  });
});
