using System;
namespace RPG.Network.Dto
{
    [Serializable]
    public class PlayerProfileResponse
    {
        public int id;
        public int userId;
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

    [Serializable]
    public class CreatePlayerProfileRequest
    {
        public int userId;
        public string characterName;
    }

    [Serializable]
    public class SyncRequest
    {
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
        public string currentZone;
        public float positionX;
        public float positionY;
        public string questFlags;
        public int totalCombats;
        public int combatsWon;
        public int combatsLost;
        public long totalDamageDealt;
        public long totalDamageTaken;
        public int totalPlaytimeMinutes;
    }

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