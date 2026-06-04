namespace RPG_ESI07.Domain;

public static class Constants
{
    #region Roles
    public const string RoleAdmin = "Admin";
    public const string RolePlayer = "Player";
    #endregion

    #region Enemy Types
    public const string EnemyTypeBasic = "basic";
    public const string EnemyTypeMiniboss = "miniboss";
    public const string EnemyTypeBoss = "boss";
    #endregion

    #region Item Types
    public const string ItemTypeWeapon = "weapon";
    public const string ItemTypeArmor = "armor";
    public const string ItemTypeAccessory = "accessory";
    public const string ItemTypeConsumable = "consumable";
    #endregion

    #region Item Categories
    public const string ItemCategoryPotionHp = "potion_hp";
    public const string ItemCategoryPotionMp = "potion_mp";
    public const string ItemCategoryElixir = "elixir";
    #endregion

    #region Skill Effects
    public const string EffectDamage = "damage";
    public const string EffectHeal = "heal";
    public const string EffectBuff = "buff";
    public const string EffectDebuff = "debuff";
    #endregion

    #region Elements
    public const string ElementNeutral = "neutral";
    public const string ElementFire = "fire";
    public const string ElementIce = "ice";
    public const string ElementLightning = "lightning";
    #endregion

    #region Database
    public const string JsonbColumnType = "jsonb"; 
    #endregion

    #region Seed
    public const string SeedIpAddress = "127.0.0.1";
    public const string SeedUserAgent = "Mozilla/5.0";
    #endregion

    #region NPC Types
    public const string NpcTypeNeutral = "neutral";
    public const string NpcTypeMerchant = "merchant";
    public const string NpcTypeQuest = "quest";
    public const string NpcTypeAlly = "ally";
    #endregion

    #region NPC States
    public const string NpcStateSerein = "SEREIN";
    public const string NpcStateApeure = "APEURÉ";
    public const string NpcStateSoulage = "SOULAGÉ";
    public const string NpcStatePanique = "PANIQUÉ";
    #endregion

    #region Enemy States
    public const string EnemyStateRepos = "REPOS";
    public const string EnemyStatePatrouille = "PATROUILLE";
    public const string EnemyStateChasse = "CHASSE";
    public const string EnemyStateFuite = "FUITE";
    #endregion

    #region Combat States
    public const string CombatStateAgressif = "AGRESSIF";
    public const string CombatStateDefensif = "DÉFENSIF";
    public const string CombatStatePanique = "PANIQUÉ";
    public const string CombatStateEnrage = "ENRAGÉ";
    #endregion

    #region Companion States
    public const string CompanionStateRepos = "REPOS";
    public const string CompanionStateJeu = "JEU";
    public const string CompanionStateManger = "MANGER";
    public const string CompanionStateExcite = "EXCITE";
    public const string CompanionStateTriste = "TRISTE";
    public const string CompanionStateEndormi = "ENDORMI";
    #endregion
}