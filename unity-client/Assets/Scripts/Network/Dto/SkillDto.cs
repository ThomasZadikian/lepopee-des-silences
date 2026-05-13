using System;

[Serializable]
public class SkillData
{
    public int id;
    public string name;
    public int mpCost;
    public int baseDamage;
    public int healAmount;
    public string effectType;   // "damage", "heal", "buff", "debuff"
    public string elementType;  // "fire", "lightning", "ice", "neutral"
    public string description;
}

[Serializable]
public class PlayerSkillResponse
{
    public int id;
    public int playerId;
    public int skillId;
    public SkillData skill;
}

[Serializable]
public class PlayerSkillArrayWrapper
{
    public PlayerSkillResponse[] items;
}