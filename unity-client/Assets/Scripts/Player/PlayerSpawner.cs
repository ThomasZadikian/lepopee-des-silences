using UnityEngine;
using RPG.Core;

public class PlayerSpawner : MonoBehaviour
{
    private void Start()
    {
        if (GameManager.Instance == null) return;

        float x = GameManager.Instance.PosX;
        float y = GameManager.Instance.PosY;

        // Si position valide (pas 0,0)
        if (x != 0f || y != 0f)
            transform.position = new Vector3(x, y, 0f);
    }
}