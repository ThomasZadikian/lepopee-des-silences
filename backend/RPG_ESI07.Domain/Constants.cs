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
}