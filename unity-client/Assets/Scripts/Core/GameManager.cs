using RPG.Network.Dto;
using UnityEngine;
namespace RPG.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        // Données du personnage (chargées depuis l'API, modifiées en session) ■
        public PlayerData Player { get; private set; } = new PlayerData();
        // Statistiques de combat de la session courante
        // Accumulées pendant la partie, réinitialisées après chaque Sync.
        public SessionStats Session { get; private set; } = new SessionStats();
        // Position et zone courante
        public string CurrentZone { get; set; } = "Zone_Depart";
        public float PosX { get; set; } = 0f;
        public float PosY { get; set; } = 0f;
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        // Initialisation depuis la réponse API
        // Appelé par PlayerService.LoadProfileAsync() après chaque login.
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
            // Réinitialiser les stats de session (nouvelle partie)
            Session = new SessionStats();
        }
        // Construit le payload de sync
        // Rassemble l'état courant pour l'envoyer à POST /api/playerprofile/sync.
        public SyncRequest BuildSyncPayload()
        {
            return new SyncRequest
            {
                // État du personnage (tel qu'il est en mémoire — peut différer de la BDD)
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
                questFlags = {}, 
                // Position dans le monde
                currentZone = CurrentZone,
                positionX = PosX,
                positionY = PosY,
                // Stats de combat accumulées depuis le dernier sync
                totalCombats = Session.TotalCombats,
                combatsWon = Session.CombatsWon,
                combatsLost = Session.CombatsLost,
                totalDamageDealt = Session.TotalDamageDealt,
                totalDamageTaken = Session.TotalDamageTaken,
                totalPlaytimeMinutes = Session.PlaytimeMinutes,
            };
        }
        // Enregistrer le résultat d'un combat
        // Appelé par CombatSystem à la fin de chaque combat.
        public void RecordCombatResult(bool won, long dmgDealt, long dmgTaken)
        {
            Session.TotalCombats++;
            if (won) Session.CombatsWon++;
            else Session.CombatsLost++;
            Session.TotalDamageDealt += dmgDealt;
            Session.TotalDamageTaken += dmgTaken;
        }
        // Appliquer des dégâts au joueur
        public void TakeDamage(int dmg)
        {
            Player.CurrentHP = Mathf.Max(0, Player.CurrentHP - dmg);
        }
        // Vérifier si le joueur est mort
        public bool IsPlayerDead => Player.CurrentHP <= 0;
    }
    // Modèles de données locaux (pas de logique — juste des champs)
    public class PlayerData
    {
        public int Id, UserId;
        public string CharacterName;
        public int Level, CurrentHP, MaxHP, CurrentMP, MaxMP;
        public int Strength, Intelligence, Speed, Experience, Gold;
    }
    public class SessionStats
    {
        public int TotalCombats, CombatsWon, CombatsLost, PlaytimeMinutes;
        public long TotalDamageDealt, TotalDamageTaken;
    }
}