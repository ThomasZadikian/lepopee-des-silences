// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';

const auth = vi.hoisted(() => ({ restoreAuthenticatedSession: vi.fn() }));
const api = vi.hoisted(() => ({ refreshSession: vi.fn() }));

vi.mock('../../features/account/authSession', () => auth);
vi.mock('../../shared/api/playerApi', () => ({ playerApi: api }));

import { requireAuthenticatedSession, router } from './index';

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
  beforeEach(() => {
    vi.clearAllMocks();
    auth.restoreAuthenticatedSession.mockResolvedValue(null);
  });

  it.each(expectedRoutes)('exposes %s at %s', (name, path) => {
    const route = router.getRoutes().find((candidate) => candidate.name === name);

    expect(route, `${String(name)} route must exist`).toBeDefined();
    expect(route?.path).toBe(path);
  });

  it('routes the root through the authenticated character gate', () => {
    const root = router.getRoutes().find((candidate) => candidate.path === '/');
    expect(root?.redirect).toEqual({ name: 'character-selection' });
  });

  it.each(['character-selection', 'account', 'threshold', 'run'])('protects the %s route', (name) => {
    const route = router.getRoutes().find((candidate) => candidate.name === name);
    expect(route?.meta.requiresAuth).toBe(true);
  });

  it('lets public routes pass without refreshing the session', async () => {
    await expect(requireAuthenticatedSession({
      meta: {},
      name: 'login',
      fullPath: '/connexion',
    })).resolves.toBe(true);
    expect(auth.restoreAuthenticatedSession).not.toHaveBeenCalled();
  });

  it('lets protected routes pass when the refresh cookie restores the session', async () => {
    auth.restoreAuthenticatedSession.mockResolvedValueOnce({ accountId: 'account-id' });

    await expect(requireAuthenticatedSession({
      meta: { requiresAuth: true },
      name: 'threshold',
      fullPath: '/palais',
    })).resolves.toBe(true);
    expect(auth.restoreAuthenticatedSession).toHaveBeenCalledWith(api.refreshSession);
  });

  it('redirects an unauthenticated gameplay route back to login with its return path', async () => {
    await expect(requireAuthenticatedSession({
      meta: { requiresAuth: true },
      name: 'threshold',
      fullPath: '/palais',
    })).resolves.toEqual({ name: 'login', query: { redirect: '/palais' } });
  });

  it('redirects the character gate without creating a redirect loop', async () => {
    await expect(requireAuthenticatedSession({
      meta: { requiresAuth: true },
      name: 'character-selection',
      fullPath: '/personnages',
    })).resolves.toEqual({ name: 'login', query: {} });
  });
});
