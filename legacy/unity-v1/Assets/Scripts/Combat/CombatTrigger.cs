using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

// CombatTrigger — détecte la collision avec le joueur et lance le combat.
// Avant de charger la scène de combat, appelle /api/enemies/{id}/scaled
// pour récupérer les stats dynamiques de l'ennemi selon le niveau du joueur.
public class CombatTrigger : MonoBehaviour
{
    [SerializeField] private int enemyId = 1;
    [SerializeField] private int instanceId = 1;
    [SerializeField] private float immunityDuration = 3f;

    private float _immunityTimer = 0f;
    private bool _entering = false;

    private void Start()
    {
        // Désactiver si l'ennemi est déjà mort
        if (RPG.Core.GameManager.Instance.DeadEnemies.Contains(instanceId))
        {
            gameObject.SetActive(false);
            return;
        }
        _immunityTimer = immunityDuration;
    }

    private void Update()
    {
        if (_immunityTimer > 0f) _immunityTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_entering) return;
        if (_immunityTimer > 0f) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        var pos = collision.transform.position;
        RPG.Core.GameManager.Instance.PosX = pos.x;
        RPG.Core.GameManager.Instance.PosY = pos.y;
        RPG.Core.GameManager.Instance.CurrentEnemyInstanceId = instanceId;

        _entering = true;
        _ = StartCombatAsync();
    }

    // Appel au backend pour récupérer les stats scalées
    // puis chargement de la scène de combat
    private async Task StartCombatAsync()
    {
        var gm = RPG.Core.GameManager.Instance;

        // 1. D'abord récupérer les stats de base (si pas encore chargé)
        var baseEnemy = EnemyService.Instance.GetEnemyById(enemyId);
        if (baseEnemy == null)
        {
            Debug.LogWarning($"[Combat] Ennemi ID {enemyId} introuvable.");
            _entering = false;
            return;
        }

        // 2. Appel scaling — playerLevel + equipmentBonus (simplifié à 0 ici,
        //    le frontend calcule le bonus équipement, Unity utilise juste le niveau)
        EnemyResponse scaledEnemy = null;
        try
        {
            int playerLevel = gm.Player.Level;
            var wrapper = await RPG.Network.ApiClient.Instance
                .GetAsync<ScaledEnemyWrapper>(
                    $"/api/enemies/{enemyId}/scaled?playerLevel={playerLevel}&equipmentBonus=0"
                );
            scaledEnemy = wrapper?.enemy;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Combat] Scaling échoué, stats de base utilisées : {e.Message}");
        }

        // 3. Fallback sur les stats de base si le scaling échoue
        gm.CurrentEnemy = scaledEnemy ?? baseEnemy;

        // 4. Charger la scène appropriée
        // L'ID 10 correspond au dernier boss dans le seeder (Archiliches Tenebris)
        // Ajuster selon ta configuration de scènes Unity
        if (baseEnemy.type == "boss")
            SceneManager.LoadScene("BossCombatScene");
        else
            SceneManager.LoadScene("CombatScene");
    }
}