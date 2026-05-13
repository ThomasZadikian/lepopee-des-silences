using RPG.Network.Dto;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerData Player { get; private set; } = new PlayerData();
        public Sprite PlayerSprite { get; set; } = null;
        public SessionStats Session { get; private set; } = new SessionStats();
        public EnemyResponse CurrentEnemy { get; set; } = null;
        public List<int> DeadEnemies { get; private set; } = new List<int>();
        public int CurrentEnemyInstanceId { get; set; } = -1;

        public string CurrentZone { get; set; } = "Zone_Depart";
        public float PosX { get; set; } = 0f;
        public float PosY { get; set; } = 0f;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetPlayer(PlayerProfileResponse profile)
        {
            Player.Id = profile.id;
            Player.UserId = profile.userId;
            Player.CharacterName = profile.characterName;
            Player.Level = profile.level;
            Player.CurrentHP = profile.currentHP;
            Player.MaxHP = profile.maxHP;
            Player.CurrentMP = profile.currentMP;
            Player.MaxMP = profile.maxMP;
            Player.Strength = profile.strength;
            Player.Intelligence = profile.intelligence;
            Player.Speed = profile.speed;
            Player.Experience = profile.experience;
            Player.Gold = profile.gold;
            Session = new SessionStats();
        }

        public SyncRequest BuildSyncPayload()
        {
            return new SyncRequest
            {
                level = Player.Level,
                currentHP = Player.CurrentHP,
                maxHP = Player.MaxHP,
                currentMP = Player.CurrentMP,
                maxMP = Player.MaxMP,
                strength = Player.Strength,
                intelligence = Player.Intelligence,
                speed = Player.Speed,
                experience = Player.Experience,
                gold = Player.Gold,
                questFlags = "{}",
                currentZone = CurrentZone,
                positionX = PosX,
                positionY = PosY,
                totalCombats = Session.TotalCombats,
                combatsWon = Session.CombatsWon,
                combatsLost = Session.CombatsLost,
                totalDamageDealt = Session.TotalDamageDealt,
                totalDamageTaken = Session.TotalDamageTaken,
                totalPlaytimeMinutes = Session.PlaytimeMinutes,
            };
        }

        public void RecordCombatResult(bool won, long dmgDealt, long dmgTaken)
        {
            Session.TotalCombats++;
            if (won) Session.CombatsWon++;
            else Session.CombatsLost++;
            Session.TotalDamageDealt += dmgDealt;
            Session.TotalDamageTaken += dmgTaken;
        }

        public void RegisterDeadEnemy(int enemyInstanceId)
        {
            if (!DeadEnemies.Contains(enemyInstanceId))
                DeadEnemies.Add(enemyInstanceId);
        }

        public void TakeDamage(int dmg)
        {
            Player.CurrentHP = Mathf.Max(0, Player.CurrentHP - dmg);
        }

        public bool IsPlayerDead => Player.CurrentHP <= 0;
    }

    public class PlayerData
    {
        public int Id, UserId;
        public string CharacterName;
        public int Level, CurrentHP, MaxHP, CurrentMP, MaxHP2, MaxMP;
        public int Strength, Intelligence, Speed, Experience, Gold;
    }

    public class SessionStats
    {
        public int TotalCombats, CombatsWon, CombatsLost, PlaytimeMinutes;
        public long TotalDamageDealt, TotalDamageTaken;
    }
}