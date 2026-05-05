using System;
namespace RPG.Network.Dto
{
    // Envoyé vers POST /api/auth/login
    [Serializable]
    public class LoginRequest
    {
        public string username; // Correspond à "username" dans le JSON
        public string password; // Correspond à "password" dans le JSON
    }
    // Envoyé vers POST /api/auth/register
    [Serializable]
    public class RegisterRequest
    {
        public string username;
        public string email;
        public string password;
    }
    // Reçu depuis POST /api/auth/login
    // Le token JWT est la valeur la plus importante ici.
    // Il sera stocké dans ApiClient et envoyé dans chaque requête suivante.
    [Serializable]
    public class LoginResponse
    {
        public bool success;
        public string token; // Le JWT — durée de vie définie dans ton backend
        public string message; // Message d'erreur si success == false
    }
    // Reçu depuis POST /api/auth/register
    [Serializable]
    public class RegisterResponse
    {
        public bool success;
        public int userId; // Nécessaire pour créer le PlayerProfile ensuite
        public string message;
    }
}