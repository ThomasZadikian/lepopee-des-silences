using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatTrigger : MonoBehaviour
{
    [SerializeField] private int enemyId = 2;
    [SerializeField] private int instanceId = 1; // ID unique par ennemi dans la scène
    private bool _isDead = false;

    private void Start()
    {
        // Si cet ennemi est déjà mort, se désactiver
        if (RPG.Core.GameManager.Instance.DeadEnemies.Contains(instanceId))
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isDead) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        var enemy = EnemyService.Instance.GetEnemyById(enemyId);
        if (enemy == null)
        {
            Debug.LogWarning($"[Combat] Ennemi ID {enemyId} introuvable.");
            return;
        }

        // Stocker l'ennemi dans GameManager et charger CombatScene
        RPG.Core.GameManager.Instance.CurrentEnemy = enemy;
        var pos = collision.transform.position;
        RPG.Core.GameManager.Instance.PosX = pos.x;
        RPG.Core.GameManager.Instance.PosY = pos.y;
        RPG.Core.GameManager.Instance.CurrentEnemyInstanceId = instanceId;
        SceneManager.LoadScene("CombatScene");
    }

    public void Die()
    {
        _isDead = true;
        gameObject.SetActive(false);
    }
}