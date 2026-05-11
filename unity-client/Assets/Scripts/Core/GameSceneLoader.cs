using System.Threading.Tasks;
using UnityEngine;

public class GameSceneLoader : MonoBehaviour
{
    private async void Start()
    {
        await EnemyService.Instance.LoadEnemiesAsync();
        Debug.Log("[Scene] Ennemis chargés.");
    }
}