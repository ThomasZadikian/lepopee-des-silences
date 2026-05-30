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
    public string initialState;
    public float influenceRadius;
    // Champs scaling — remplis uniquement depuis /api/enemies/{id}/scaled
    public float multiplier;
    public int scaledMaxHP;
    public int scaledStrength;
    public int scaledIntelligence;
    public int scaledSpeed;
}

[Serializable]
public class EnemyArrayWrapper
{
    public EnemyResponse[] items;
}

// Wrapper pour la réponse de /api/enemies/{id}/scaled
[Serializable]
public class ScaledEnemyWrapper
{
    public EnemyResponse enemy;
}