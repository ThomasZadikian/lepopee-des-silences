using UnityEngine;

[CreateAssetMenu(fileName = "EnemySpriteDatabase", menuName = "RPG/Enemy Sprite Database")]
public class EnemySpriteDatabase : ScriptableObject
{
    [System.Serializable]
    public class EnemySpriteEntry
    {
        public int enemyId;
        public Sprite sprite;
    }

    public EnemySpriteEntry[] entries;

    public Sprite GetSprite(int enemyId)
    {
        foreach (var entry in entries)
            if (entry.enemyId == enemyId) return entry.sprite;
        return null;
    }
}