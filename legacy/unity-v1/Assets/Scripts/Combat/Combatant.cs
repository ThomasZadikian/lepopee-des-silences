using UnityEngine;

[System.Serializable]
public class Combatant
{
    public string name;
    public int currentHP;
    public int maxHP;
    public int currentMP;
    public int maxMP;
    public float speed;
    public float atbCurrent;
    public bool isPlayer;
    public Color portraitColor;
    public Sprite portrait;
    public float physicalResistance = 1f;
    public int strength = 0;

    public GameObject combatantObject;
    public RectTransform uiPortrait;

    public bool IsATBFull => atbCurrent >= 100f;
    public bool IsDead => currentHP <= 0;

    public static Combatant FromPlayer(RPG.Core.PlayerData p)
    {
        return new Combatant
        {
            name = p.CharacterName,
            currentHP = p.CurrentHP,
            maxHP = p.MaxHP,
            currentMP = p.CurrentMP,
            maxMP = p.MaxMP,
            speed = p.Speed,
            atbCurrent = 0f,
            isPlayer = true,
            portraitColor = Color.white,
        };
    }

    public static Combatant FromEnemy(EnemyResponse e)
    {
        // Lire les stats scalées si disponibles, sinon fallback sur stats de base
        int hp = e.scaledMaxHP > 0 ? e.scaledMaxHP : e.maxHP;
        int str = e.scaledStrength > 0 ? e.scaledStrength : e.strength;
        float spd = e.scaledSpeed > 0 ? e.scaledSpeed : e.speed;

        return new Combatant
        {
            name = e.name,
            currentHP = hp,
            maxHP = hp,
            currentMP = 0,
            maxMP = 0,
            speed = spd * 1.4f,
            atbCurrent = 0f,
            isPlayer = false,
            portraitColor = Color.red,
            physicalResistance = e.physicalResistance > 0 ? e.physicalResistance : 1f,
            strength = str,
        };
    }
}