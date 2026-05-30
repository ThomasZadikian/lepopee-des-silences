using System;
namespace RPG.Network.Dto
{
    // ── Login ─────────────────────────────────────────────────────
    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    // Réponse login — inclut les champs MFA
    [Serializable]
    public class LoginResponse
    {
        public bool success;
        public string token;
        public bool requiresMfa;
        public bool requiresMfaSetup;
        public string message;
        public int userId;
    }

    // ── Register ──────────────────────────────────────────────────
    [Serializable]
    public class RegisterRequest
    {
        public string username;
        public string email;
        public string password;
    }

    [Serializable]
    public class RegisterResponse
    {
        public bool success;
        public string token;
        public bool requiresMfaSetup;
        public string message;
        public int userId;
    }

    // ── MFA Setup ─────────────────────────────────────────────────
    [Serializable]
    public class SetupMfaRequest
    {
        public int userId;
        public string token; // mfaToken reçu lors du login
    }

    [Serializable]
    public class SetupMfaResponse
    {
        public bool success;
        public string qrCodeUri;
        public string manualKey;
        public string message;
    }

    // ── MFA Verify (setup) ────────────────────────────────────────
    [Serializable]
    public class VerifyMfaRequest
    {
        public int userId;
        public string token;  // mfaToken
        public string code;   // code TOTP 6 chiffres
    }

    // ── MFA Login ─────────────────────────────────────────────────
    [Serializable]
    public class LoginMfaRequest
    {
        public int userId;
        public string code;   // code TOTP 6 chiffres
    }

    // Réponse commune auth (verify + login MFA)
    [Serializable]
    public class AuthResponse
    {
        public bool success;
        public string token;
        public bool requiresMfa;
        public string message;
    }
}