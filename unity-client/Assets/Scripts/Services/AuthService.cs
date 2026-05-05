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
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        // Login
        // Retourne le token JWT si succès, null sinon.
        // Le token est automatiquement stocké dans ApiClient.Instance
        // — toutes les requêtes suivantes l'utiliseront sans action supplémentaire.
        public async Task<string> LoginAsync(string username, string password)
        {
            try
            {
                var response = await ApiClient.Instance.PostAsync<LoginResponse>(
                "/api/auth/login",
                new LoginRequest { username = username, password = password }
                );
                if (response.success && !string.IsNullOrEmpty(response.token))
                {
                    // Stockage du JWT dans ApiClient — il sera injecté automatiquement
                    // dans le header "Authorization: Bearer ..." de toutes les requêtes.
                    ApiClient.Instance.SetToken(response.token);
                    Debug.Log("[Auth] Login réussi. Token stocké.");
                    return response.token;
                }
                Debug.LogWarning($"[Auth] Login refusé : {response.message}");
                return null;
            }
            catch (Exception e)
            {
                // L'API est peut-être inaccessible (Docker pas lancé, mauvaise URL...)
                Debug.LogError($"[Auth] Exception lors du login : {e.Message}");
                return null;
            }
        }
        // Register
        // Retourne l'userId (int > 0) si succès, -1 sinon.
        // Après un register réussi, il faut appeler LoginAsync puis CreateProfileAsync.
        public async Task<int> RegisterAsync(string username, string email, string password)
        {
            try
            {
                var response = await ApiClient.Instance.PostAsync<RegisterResponse>(
                "/api/auth/register",
                new RegisterRequest
                {
                    username = username,
                    email = email,
                    password = password
                }
                );
                if (response.success)
                {
                    Debug.Log($"[Auth] Register réussi. UserId = {response.userId}");
                    return response.userId;
                }
                Debug.LogWarning($"[Auth] Register refusé : {response.message}");
                return -1;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Auth] Exception lors du register : {e.Message}");
                return -1;
            }
        }
        // Logout
        // Efface le token. La prochaine requête recevra 401 Unauthorized.
        public void Logout()
        {
            ApiClient.Instance.ClearToken();
            Debug.Log("[Auth] Déconnexion — token effacé.");
            // Charger la scène de login :
            // SceneManager.LoadScene("LoginScene");
        }
    }
}