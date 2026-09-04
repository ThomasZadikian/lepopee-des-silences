export type AuthenticatedSession = {
  accountId: string;
  sessionId: string;
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  recoveryCodes?: string[] | null;
};

const challengeStorageKey = 'leds.auth-challenge';
let authenticatedSession: AuthenticatedSession | null = null;

function browserSessionStorage(): Storage | null {
  return typeof window !== 'undefined' && window.sessionStorage
    ? window.sessionStorage
    : null;
}

export function setAuthenticatedSession(session: AuthenticatedSession): void {
  authenticatedSession = session;
}

export function clearAuthenticatedSession(): void {
  authenticatedSession = null;
}

export function getAuthenticatedSession(): AuthenticatedSession | null {
  return authenticatedSession;
}

export function getAuthenticatedAccountId(): string | null {
  return authenticatedSession?.accountId ?? null;
}

export function getAccessToken(): string | null {
  return authenticatedSession?.accessToken ?? null;
}

export async function restoreAuthenticatedSession(
  refreshSession: () => Promise<AuthenticatedSession>,
): Promise<AuthenticatedSession | null> {
  if (authenticatedSession) return authenticatedSession;

  try {
    const session = await refreshSession();
    setAuthenticatedSession(session);
    return session;
  } catch {
    clearAuthenticatedSession();
    return null;
  }
}

export function getRecoveryCodes(): readonly string[] {
  return authenticatedSession?.recoveryCodes ?? [];
}

export function setChallengeToken(token: string | null): void {
  const storage = browserSessionStorage();
  if (!storage) return;

  if (token) storage.setItem(challengeStorageKey, token);
  else storage.removeItem(challengeStorageKey);
}

export function getChallengeToken(): string | null {
  return browserSessionStorage()?.getItem(challengeStorageKey) ?? null;
}
