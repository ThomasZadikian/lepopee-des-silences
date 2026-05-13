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
    public float atbCurrent;   // 0 → 100
    public bool isPlayer;
    public Color portraitColor; // Rouge ennemi, blanc joueur
    public Sprite portrait;      // null pour l'instant → carré coloré

    // Référence Unity (sprite dans la scène)
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
            speed = e.speed,
            atbCurrent = 0f,
            isPlayer = false,
            portraitColor = Color.red,
        };
    }
}