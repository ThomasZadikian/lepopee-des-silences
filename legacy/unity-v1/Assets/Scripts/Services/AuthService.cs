using RPG.Network;
using RPG.Network.Dto;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace RPG.Services
{
    public class AuthService : MonoBehaviour
    {
        public static AuthService Instance { get; private set; }

        // Token MFA temporaire conservé entre les étapes login→verify
        public string PendingMfaToken { get; private set; }
        public int PendingUserId { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Login ─────────────────────────────────────────────────
        // Retourne un AuthFlowResult indiquant quoi faire ensuite.
        public async Task<AuthFlowResult> LoginAsync(string username, string password)
        {
            try
            {
                var response = await ApiClient.Instance.PostAsync<LoginResponse>(
                    "/api/auth/login",
                    new LoginRequest { username = username, password = password }
                );

                if (!response.success)
                {
                    Debug.LogWarning($"[Auth] Login refusé : {response.message}");
                    return new AuthFlowResult { State = AuthState.Failed, Message = response.message };
                }

                // MFA setup requis (premier login)
                if (response.requiresMfaSetup)
                {
                    PendingMfaToken = response.token;
                    PendingUserId = response.userId;
                    return new AuthFlowResult { State = AuthState.NeedsMfaSetup };
                }

                // MFA vérification requise (compte déjà configuré)
                if (response.requiresMfa)
                {
                    PendingMfaToken = response.token;
                    PendingUserId = response.userId;
                    return new AuthFlowResult { State = AuthState.NeedsMfaVerify };
                }

                // Login direct (ne devrait plus arriver avec MFA obligatoire)
                ApiClient.Instance.SetToken(response.token);
                return new AuthFlowResult { State = AuthState.Success, Token = response.token };
            }
            catch (Exception e)
            {
                Debug.LogError($"[Auth] Exception login : {e.Message}");
                return new AuthFlowResult { State = AuthState.Failed, Message = "Erreur réseau." };
            }
        }

        // ── MFA Setup — récupère le QR code ──────────────────────
        public async Task<SetupMfaResponse> SetupMfaAsync()
        {
            try
            {
                return await ApiClient.Instance.PostAsync<SetupMfaResponse>(
                    "/api/auth/mfa/setup",
                    new SetupMfaRequest { userId = PendingUserId, token = PendingMfaToken }
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"[Auth] SetupMfa : {e.Message}");
                return null;
            }
        }

        // ── MFA Verify (après setup) ──────────────────────────────
        public async Task<AuthFlowResult> VerifyMfaSetupAsync(string totpCode)
        {
            try
            {
                var response = await ApiClient.Instance.PostAsync<AuthResponse>(
                    "/api/auth/mfa/verify",
                    new VerifyMfaRequest
                    {
                        userId = PendingUserId,
                        token = PendingMfaToken,
                        code = totpCode
                    }
                );

                if (!response.success)
                    return new AuthFlowResult { State = AuthState.Failed, Message = response.message };

                ApiClient.Instance.SetToken(response.token);
                ClearPending();
                return new AuthFlowResult { State = AuthState.Success, Token = response.token };
            }
            catch (Exception e)
            {
                Debug.LogError($"[Auth] VerifyMfaSetup : {e.Message}");
                return new AuthFlowResult { State = AuthState.Failed, Message = "Erreur réseau." };
            }
        }

        // ── MFA Login — code TOTP pour connexion ──────────────────
        public async Task<AuthFlowResult> LoginWithMfaAsync(string totpCode)
        {
            try
            {
                var response = await ApiClient.Instance.PostAsync<AuthResponse>(
                    "/api/auth/mfa",
                    new LoginMfaRequest { userId = PendingUserId, code = totpCode }
                );

                if (!response.success)
                    return new AuthFlowResult { State = AuthState.Failed, Message = response.message };

                ApiClient.Instance.SetToken(response.token);
                ClearPending();
                return new AuthFlowResult { State = AuthState.Success, Token = response.token };
            }
            catch (Exception e)
            {
                Debug.LogError($"[Auth] LoginWithMfa : {e.Message}");
                return new AuthFlowResult { State = AuthState.Failed, Message = "Erreur réseau." };
            }
        }

        // ── Register ──────────────────────────────────────────────
        public async Task<AuthFlowResult> RegisterAsync(string username, string email, string password)
        {
            try
            {
                var response = await ApiClient.Instance.PostAsync<RegisterResponse>(
                    "/api/auth/register",
                    new RegisterRequest { username = username, email = email, password = password }
                );

                if (!response.success)
                    return new AuthFlowResult { State = AuthState.Failed, Message = response.message };

                // Register déclenche aussi le MFA setup
                if (response.requiresMfaSetup)
                {
                    PendingMfaToken = response.token;
                    PendingUserId = response.userId;
                    return new AuthFlowResult { State = AuthState.NeedsMfaSetup };
                }

                return new AuthFlowResult { State = AuthState.Success };
            }
            catch (Exception e)
            {
                Debug.LogError($"[Auth] Register : {e.Message}");
                return new AuthFlowResult { State = AuthState.Failed, Message = "Erreur réseau." };
            }
        }

        public void Logout()
        {
            ApiClient.Instance.ClearToken();
            ClearPending();
        }

        private void ClearPending()
        {
            PendingMfaToken = null;
            PendingUserId = 0;
        }
    }

    // ── Types de flux auth ─────────────────────────────────────────
    public enum AuthState
    {
        Success,
        NeedsMfaSetup,
        NeedsMfaVerify,
        Failed
    }

    public class AuthFlowResult
    {
        public AuthState State;
        public string Token;
        public string Message;
    }
}