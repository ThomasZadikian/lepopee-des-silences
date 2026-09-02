// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';

import { router } from './index';

const expectedRoutes = [
  ['login', '/connexion'],
  ['register', '/inscription'],
  ['verify-email', '/verification-email'],
  ['mfa-setup', '/securite/mfa/configuration'],
  ['mfa-challenge', '/securite/mfa'],
  ['password-recovery', '/mot-de-passe-oublie'],
  ['password-reset', '/reinitialisation-mot-de-passe'],
  ['character-selection', '/personnages'],
  ['account', '/compte'],
] as const;

describe('Account/Auth route contract', () => {
  it.each(expectedRoutes)('exposes %s at %s', (name, path) => {
    const route = router.getRoutes().find((candidate) => candidate.name === name);

    expect(route, `${String(name)} route must exist`).toBeDefined();
    expect(route?.path).toBe(path);
  });

  it('does not make the gameplay threshold the public entry point anymore', () => {
    const root = router.getRoutes().find((candidate) => candidate.path === '/');
    expect(root?.redirect).toEqual({ name: 'login' });
  });
});
