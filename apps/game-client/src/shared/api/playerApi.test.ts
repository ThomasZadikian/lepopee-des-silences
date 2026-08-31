import { beforeEach, describe, expect, it, vi } from 'vitest';

import { playerApi } from './playerApi';

const fetchMock = vi.fn();
vi.stubGlobal('fetch', fetchMock);

function response(body: unknown = {}, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    headers: new Headers({ 'content-type': 'application/json' }),
    json: vi.fn().mockResolvedValue(body),
    text: vi.fn().mockResolvedValue(typeof body === 'string' ? body : JSON.stringify(body)),
  } as unknown as Response;
}

describe('playerApi', () => {
  beforeEach(() => {
    fetchMock.mockReset();
    fetchMock.mockResolvedValue(response({}));
  });

  it('registers an account against the Player service with cookies enabled', async () => {
    fetchMock.mockResolvedValueOnce(response({ accountId: 'account', emailVerificationRequired: true }, 201));

    await playerApi.registerAccount({
      displayName: 'Nocturne',
      email: 'player@example.fr',
      password: 'correcthorse',
      ageConfirmed: true,
    });

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/api/v2/account/register'),
      expect.objectContaining({
        method: 'POST',
        credentials: 'include',
        body: JSON.stringify({
          displayName: 'Nocturne',
          email: 'player@example.fr',
          password: 'correcthorse',
          ageConfirmed: true,
        }),
      }),
    );
  });

  it('uses the expected anonymous authentication endpoints', async () => {
    await playerApi.verifyEmail('verify-token');
    await playerApi.beginLogin('player@example.fr', 'correcthorse');
    await playerApi.beginMfaEnrollment('challenge');
    await playerApi.confirmMfaEnrollment('challenge', '123456');
    await playerApi.completeMfaChallenge('challenge', '123456');
    await playerApi.requestPasswordReset('player@example.fr');
    await playerApi.resetPassword('reset-token', 'new-password-12');

    const urls = fetchMock.mock.calls.map(([url]) => String(url));
    expect(urls).toEqual(expect.arrayContaining([
      expect.stringContaining('/verify-email'),
      expect.stringContaining('/login'),
      expect.stringContaining('/mfa/enrollment'),
      expect.stringContaining('/mfa/confirm'),
      expect.stringContaining('/mfa/challenge'),
      expect.stringContaining('/password-recovery'),
      expect.stringContaining('/password-reset'),
    ]));
    expect(fetchMock.mock.calls.every(([, options]) => options.credentials === 'include')).toBe(true);
  });

  it('adds the bearer access token to protected character creation', async () => {
    await playerApi.createCharacter('access-token', {
      displayName: 'Aster',
      archetypeKey: 'archetype.porteur',
    });

    expect(fetchMock).toHaveBeenLastCalledWith(
      expect.stringContaining('/api/v2/account/characters'),
      expect.objectContaining({
        method: 'POST',
        credentials: 'include',
        headers: expect.objectContaining({ Authorization: 'Bearer access-token' }),
      }),
    );
  });

  it('refreshes the session exclusively through the HttpOnly refresh cookie', async () => {
    await playerApi.refreshSession();

    expect(fetchMock).toHaveBeenLastCalledWith(
      expect.stringContaining('/api/v2/account/refresh'),
      expect.objectContaining({ method: 'POST', credentials: 'include' }),
    );
    const options = fetchMock.mock.calls.at(-1)?.[1] as RequestInit;
    expect(options.headers).not.toHaveProperty('Authorization');
  });
});
