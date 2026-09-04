// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
  clearAuthenticatedSession,
  getAuthenticatedAccountId,
  getAccessToken,
  getChallengeToken,
  restoreAuthenticatedSession,
  setAuthenticatedSession,
  setChallengeToken,
} from './authSession';

describe('authSession', () => {
  beforeEach(() => {
    sessionStorage.clear();
    clearAuthenticatedSession();
    setChallengeToken(null);
  });

  it('keeps access tokens in memory only', () => {
    setAuthenticatedSession({
      accountId: 'account',
      sessionId: 'session',
      accessToken: 'access-token',
      accessTokenExpiresAtUtc: '2026-08-31T13:00:00Z',
      recoveryCodes: null,
    });

    expect(getAccessToken()).toBe('access-token');
    expect(sessionStorage.getItem('leds.access-token')).toBeNull();
  });

  it('keeps only the short-lived MFA challenge in session storage across route reloads', () => {
    setChallengeToken('challenge-token');
    expect(getChallengeToken()).toBe('challenge-token');
    expect(sessionStorage.getItem('leds.auth-challenge')).toBe('challenge-token');

    setChallengeToken(null);
    expect(getChallengeToken()).toBeNull();
    expect(sessionStorage.getItem('leds.auth-challenge')).toBeNull();
  });

  it('clears authenticated state explicitly', () => {
    setAuthenticatedSession({
      accountId: 'account',
      sessionId: 'session',
      accessToken: 'access-token',
      accessTokenExpiresAtUtc: '2026-08-31T13:00:00Z',
      recoveryCodes: ['one'],
    });
    clearAuthenticatedSession();
    expect(getAccessToken()).toBeNull();
  });

  it('restores the in-memory session through the HttpOnly refresh-cookie exchange', async () => {
    const refresh = async () => ({
      accountId: 'restored-account',
      sessionId: 'restored-session',
      accessToken: 'restored-access-token',
      accessTokenExpiresAtUtc: '2026-08-31T13:00:00Z',
      recoveryCodes: null,
    });

    const restored = await restoreAuthenticatedSession(refresh);

    expect(restored?.accessToken).toBe('restored-access-token');
    expect(getAccessToken()).toBe('restored-access-token');
    expect(getAuthenticatedAccountId()).toBe('restored-account');
  });

  it('reuses an already restored session without another refresh request', async () => {
    const session = {
      accountId: 'cached-account',
      sessionId: 'cached-session',
      accessToken: 'cached-access-token',
      accessTokenExpiresAtUtc: '2026-08-31T13:00:00Z',
      recoveryCodes: null,
    };
    setAuthenticatedSession(session);
    const refresh = vi.fn();

    await expect(restoreAuthenticatedSession(refresh)).resolves.toBe(session);
    expect(refresh).not.toHaveBeenCalled();
  });

  it('clears stale in-memory state when the refresh-cookie exchange fails', async () => {
    const refresh = vi.fn().mockRejectedValue(new Error('Refresh cookie expired'));

    await expect(restoreAuthenticatedSession(refresh)).resolves.toBeNull();
    expect(getAccessToken()).toBeNull();
    expect(getAuthenticatedAccountId()).toBeNull();
  });
});
