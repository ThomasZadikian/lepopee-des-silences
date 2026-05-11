using System;

[Serializable]
public class EnemyResponse
{
    public int id;
    public string name;
    public string type;
    public int maxHP;
    public int strength;
    public int intelligence;
    public int speed;
    public float physicalResistance;
    public float magicalResistance;
    public int experienceReward;
    public int goldReward;
    public string description;
}

[Serializable]
public class EnemyArrayWrapper
{
    public EnemyResponse[] items;
}