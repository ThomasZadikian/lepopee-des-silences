using UnityEngine;
using RPG.Core;

public class PlayerSpawner : MonoBehaviour
{
    private void Start()
    {
        if (GameManager.Instance == null) return;

        float x = GameManager.Instance.PosX;
        float y = GameManager.Instance.PosY;

        // Si pas de save → spawn au point de départ
        if (x == 0f && y == 0f)
        {
            x = 0.05f;
            y = 0.57f;
        }

        transform.position = new Vector3(x, y, 0f);
    }
}