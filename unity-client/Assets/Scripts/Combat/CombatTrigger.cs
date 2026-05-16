using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatTrigger : MonoBehaviour
{
    [SerializeField] private int enemyId = 2;
    [SerializeField] private int instanceId = 1;
    [SerializeField] private float immunityDuration = 3f;

    private bool _isDead = false;
    private float _immunityTimer = 0f;

    private void Start()
    {
        if (RPG.Core.GameManager.Instance.DeadEnemies.Contains(instanceId))
        {
            gameObject.SetActive(false);
            return;
        }

        // Immunité au spawn — évite le combat immédiat après fuite/respawn
        _immunityTimer = immunityDuration;
    }

    private void Update()
    {
        if (_immunityTimer > 0f)
            _immunityTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isDead) return;
        if (_immunityTimer > 0f) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        var enemy = EnemyService.Instance.GetEnemyById(enemyId);
        if (enemy == null)
        {
            Debug.LogWarning($"[Combat] Ennemi ID {enemyId} introuvable.");
            return;
        }

        var pos = collision.transform.position;
        RPG.Core.GameManager.Instance.PosX = pos.x;
        RPG.Core.GameManager.Instance.PosY = pos.y;
        RPG.Core.GameManager.Instance.CurrentEnemy = enemy;
        RPG.Core.GameManager.Instance.CurrentEnemyInstanceId = instanceId;

        if (enemy.id == 17)
            SceneManager.LoadScene("BossCombatScene");
        else
            SceneManager.LoadScene("CombatScene");
    }

    public void Die()
    {
        _isDead = true;
        gameObject.SetActive(false);
    }
}