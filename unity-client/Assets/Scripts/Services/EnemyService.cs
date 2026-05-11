using System;
using System.Threading.Tasks;
using UnityEngine;

public class EnemyService : MonoBehaviour
{
    public static EnemyService Instance { get; private set; }
    public EnemyResponse[] Enemies { get; private set; }

    private async void Start()
    {
        bool loaded = await EnemyService.Instance.LoadEnemiesAsync();
        Debug.Log($"[Scene] Ennemis chargés : {loaded}, count : {EnemyService.Instance.Enemies?.Length}");
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> LoadEnemiesAsync()
    {
        try
        {
            var wrapper = await RPG.Network.ApiClient.Instance
                .GetAsync<EnemyArrayWrapper>("/api/enemies");
            Enemies = wrapper.items;
            Debug.Log($"[Enemy] {Enemies.Length} ennemis chargés.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Enemy] Chargement : {e.Message}");
            return false;
        }
    }

    public EnemyResponse GetEnemyById(int id)
    {
        if (Enemies == null) return null;
        foreach (var e in Enemies)
            if (e.id == id) return e;
        return null;
    }
}