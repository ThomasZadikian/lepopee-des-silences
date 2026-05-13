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
        return new Combatant
        {
            name = e.name,
            currentHP = e.maxHP,
            maxHP = e.maxHP,
            currentMP = 0,
            maxMP = 0,
            speed = e.speed * 1.4f,
            atbCurrent = 0f,
            isPlayer = false,
            portraitColor = Color.red,
        };
    }
}