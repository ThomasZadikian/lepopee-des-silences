using System;
namespace RPG.Network.Dto
{
    // Reçu depuis GET /api/playerprofile/me
    // Contient TOUT ce qu'il faut pour initialiser la session Unity.
    [Serializable]
    public class PlayerProfileResponse
    {
        public int id; // ID du PlayerProfile (pas du User)
        public int userId; // ID du User (pour les vérifications IDOR)
        public string characterName;
        public int level;
        public int currentHP;
        public int maxHP;
        public int currentMP;
        public int maxMP;
        public int strength;
        public int intelligence;
        public int speed;
        public int experience;
        public int gold;
    }
    // Envoyé vers POST /api/playerprofile (une seule fois par compte)
    [Serializable]
    public class CreatePlayerProfileRequest
    {
        public int userId;
        public string characterName;
    }
    // Envoyé vers POST /api/playerprofile/sync
    // Ce DTO est le "payload de sauvegarde".
    // Il correspond champ par champ à SyncGameSaveCommand dans ton backend.
    // Le requestingUserId n'est PAS ici — le serveur le lit depuis le JWT.
    [Serializable]
    public class SyncRequest
    {
        // État du personnage au moment de la sauvegarde
        public int level;
        public int currentHP;
        public int maxHP;
        public int currentMP;
        public int maxMP;
        public int strength;
        public int intelligence;
        public int speed;
        public int experience;
        public int gold;
        // Position et zone dans le monde
        public string currentZone; // ex: "Zone_Foret_01"
        public float positionX;
        public float positionY;
        public string questFlags; // JSON stringifié ou null
                                  // Statistiques de combat accumulées depuis le dernier sync
        public int totalCombats;
        public int combatsWon;
        public int combatsLost;
        public long totalDamageDealt;
        public long totalDamageTaken;
        public int totalPlaytimeMinutes;
    }
    // Reçu depuis POST /api/playerprofile/sync
    [Serializable]
    public class SyncResponse
    {
        public bool success;
        public string message;
    }

    [Serializable]
    public class GameSaveResponse
    {
        public int id;
        public int playerId;
        public string currentZone;
        public float positionX;
        public float positionY;
        public string questFlags;
        public string savedAt;
    }

    [Serializable]
    public class GameSaveArrayWrapper
    {
        public GameSaveResponse[] items;
    }
}