import { httpRequestTo } from './httpClient';
import { environment } from '../config/environment';

export type RegisterAccountRequest = {
  displayName: string;
  email: string;
  password: string;
  ageConfirmed: boolean;
};

export type RegisterAccountResponse = {
  accountId: string;
  email: string;
  emailVerificationRequired: boolean;
};

export type VerifyEmailResponse = {
  accountId: string;
  verified: boolean;
};

export type BeginLoginResponse = {
  status: string;
  challengeToken?: string | null;
  emailVerificationRequired?: boolean;
};

export type MfaEnrollmentResponse = {
  challengeToken: string;
  otpAuthUri: string;
  manualEntryKey: string;
};

export type AuthenticatedSessionResponse = {
  accountId: string;
  sessionId: string;
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  recoveryCodes?: string[] | null;
};

export type CreateCharacterRequest = {
  displayName: string;
  archetypeKey: string;
};

export type GameSessionLeaseResponse = {
  status: string;
  sessionId: string;
  expiresAtUtc: string;
};

const accountRoot = '/api/v2/account';

function request<TResponse>(path: string, options: RequestInit = {}): Promise<TResponse> {
  return httpRequestTo<TResponse>(
    environment.playerApiUrl,
    path,
    { ...options, credentials: 'include' },
    'Player API',
  );
}

function post<TResponse>(path: string, body?: unknown, headers?: HeadersInit): Promise<TResponse> {
  return request<TResponse>(path, {
    method: 'POST',
    body: body === undefined ? undefined : JSON.stringify(body),
    headers,
  });
}

function authorizedHeaders(accessToken: string): HeadersInit {
  return { Authorization: `Bearer ${accessToken}` };
}

export const playerApi = {
  registerAccount: (body: RegisterAccountRequest) =>
    post<RegisterAccountResponse>(`${accountRoot}/register`, body),

  verifyEmail: (token: string) =>
    post<VerifyEmailResponse>(`${accountRoot}/verify-email`, { token }),

  beginLogin: (email: string, password: string) =>
    post<BeginLoginResponse>(`${accountRoot}/login`, { email, password }),

  beginMfaEnrollment: (challengeToken: string) =>
    post<MfaEnrollmentResponse>(`${accountRoot}/mfa/enrollment`, { challengeToken }),

  confirmMfaEnrollment: (challengeToken: string, code: string) =>
    post<AuthenticatedSessionResponse>(`${accountRoot}/mfa/confirm`, { challengeToken, code }),

  completeMfaChallenge: (challengeToken: string, code: string) =>
    post<AuthenticatedSessionResponse>(`${accountRoot}/mfa/challenge`, { challengeToken, code }),

  completeMfaRecovery: (challengeToken: string, recoveryCode: string) =>
    post<AuthenticatedSessionResponse>(`${accountRoot}/mfa/recovery`, { challengeToken, recoveryCode }),

  refreshSession: () =>
    post<AuthenticatedSessionResponse>(`${accountRoot}/refresh`),

  requestPasswordReset: (email: string) =>
    post<void>(`${accountRoot}/password-recovery`, { email }),

  resetPassword: (token: string, newPassword: string) =>
    post<void>(`${accountRoot}/password-reset`, { token, newPassword }),

  createCharacter: (accessToken: string, body: CreateCharacterRequest) =>
    post<unknown>(`${accountRoot}/characters`, body, authorizedHeaders(accessToken)),

  getAccount: (accessToken: string) =>
    request<unknown>(`${accountRoot}/me`, {
      method: 'GET',
      headers: authorizedHeaders(accessToken),
    }),

  claimGameSession: (accessToken: string, confirmTransfer = false) =>
    post<GameSessionLeaseResponse>(
      `${accountRoot}/game-session`,
      { confirmTransfer },
      authorizedHeaders(accessToken),
    ),

  heartbeatGameSession: (accessToken: string) =>
    post<GameSessionLeaseResponse>(
      `${accountRoot}/game-session/heartbeat`,
      undefined,
      authorizedHeaders(accessToken),
    ),

  releaseGameSession: (accessToken: string) =>
    request<void>(`${accountRoot}/game-session`, {
      method: 'DELETE',
      headers: authorizedHeaders(accessToken),
    }),

  logout: (accessToken: string) =>
    post<void>(`${accountRoot}/logout`, undefined, authorizedHeaders(accessToken)),
};
