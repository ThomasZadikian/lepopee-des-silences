using RPG.World;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(MarkovStateMachine))]
[RequireComponent(typeof(InfluenceDetector))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyWorldController : MonoBehaviour
{
    [Header("Ennemi")]
    [SerializeField] private int enemyId = 1;
    [SerializeField] private int instanceId = 1;
    [SerializeField] private float immunityDuration = 3f;

    [Header("Patrouille")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 1.5f;

    private MarkovStateMachine _markov;
    private InfluenceDetector _detector;
    private Rigidbody2D _rb;

    private int _patrolIndex = 0;
    private float _patrolWait = 0f;
    private bool _waitingAtPoint = false;
    private float _immunityTimer = 0f;
    private bool _entering = false;

    private Transform _playerTransform;

    private void Awake()
    {
        _markov = GetComponent<MarkovStateMachine>();
        _detector = GetComponent<InfluenceDetector>();
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    private async void Start()
    {
        if (RPG.Core.GameManager.Instance.DeadEnemies.Contains(instanceId))
        {
            gameObject.SetActive(false);
            return;
        }

        // Attendre que EnemyService ait chargé les ennemis (max 5s)
        float waited = 0f;
        while ((EnemyService.Instance == null || EnemyService.Instance.Enemies == null)
               && waited < 5f)
        {
            await Task.Delay(100);
            waited += 0.1f;
        }

        var enemy = EnemyService.Instance?.GetEnemyById(enemyId);
        if (enemy != null)
        {
            _detector.SetRadius(enemy.influenceRadius);
            Debug.Log($"[EnemyWorld] {enemy.name} initialisé (rayon {enemy.influenceRadius}).");
        }
        else
        {
            Debug.LogWarning($"[EnemyWorld] enemyId={enemyId} introuvable après attente. " +
                             $"Vérifie l'Inspector et que les ennemis sont chargés.");
        }

        _immunityTimer = immunityDuration;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _playerTransform = playerObj.transform;
    }

    private void Update()
    {
        if (_immunityTimer > 0f) _immunityTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        switch (_markov.CurrentState)
        {
            case EnemyState.Repos: _rb.linearVelocity = Vector2.zero; break;
            case EnemyState.Patrouille: Patrol(); break;
            case EnemyState.Chasse: ChasePlayer(); break;
            case EnemyState.Fuite: FleeFromPlayer(); break;
        }
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        { _rb.linearVelocity = Vector2.zero; return; }

        if (_waitingAtPoint)
        {
            _patrolWait -= Time.fixedDeltaTime;
            if (_patrolWait <= 0f)
            { _waitingAtPoint = false; _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length; }
            return;
        }

        var dir = patrolPoints[_patrolIndex].position - transform.position;
        if (dir.magnitude < 0.15f)
        { _waitingAtPoint = true; _patrolWait = patrolWaitTime; _rb.linearVelocity = Vector2.zero; return; }

        _rb.linearVelocity = dir.normalized * _markov.CurrentSpeed;
    }

    private void ChasePlayer()
    {
        if (_playerTransform == null) return;
        var dir = (_playerTransform.position - transform.position).normalized;
        _rb.linearVelocity = new Vector2(dir.x, dir.y) * _markov.CurrentSpeed;
    }

    private void FleeFromPlayer()
    {
        if (_playerTransform == null) return;
        var dir = (transform.position - _playerTransform.position).normalized;
        _rb.linearVelocity = new Vector2(dir.x, dir.y) * _markov.CurrentSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_entering) return;
        if (_immunityTimer > 0f) return;
        if (!other.CompareTag("Player")) return;
        if (_markov.CurrentState == EnemyState.Fuite) return;

        _entering = true;
        var pos = other.transform.position;
        RPG.Core.GameManager.Instance.PosX = pos.x;
        RPG.Core.GameManager.Instance.PosY = pos.y;
        RPG.Core.GameManager.Instance.CurrentEnemyInstanceId = instanceId;

        _ = StartCombatAsync();
    }

    private async Task StartCombatAsync()
    {
        var gm = RPG.Core.GameManager.Instance;

        // 1. Récupérer les stats de base depuis le cache local
        var baseEnemy = EnemyService.Instance?.GetEnemyById(enemyId);

        // 2. Tenter le scaling via API
        EnemyResponse scaledEnemy = null;
        try
        {
            int level = gm.Player.Level;
            string url = $"/api/enemies/{enemyId}/scaled?playerLevel={level}&equipmentBonus=0";

            // Essayer d'abord avec wrapper, sinon directement
            try
            {
                var wrapper = await RPG.Network.ApiClient.Instance
                    .GetAsync<ScaledEnemyWrapper>(url);
                scaledEnemy = wrapper?.enemy;
            }
            catch { }

            // Si wrapper vide, tenter désérialisation directe
            if (scaledEnemy == null)
            {
                scaledEnemy = await RPG.Network.ApiClient.Instance
                    .GetAsync<EnemyResponse>(url);
            }

            if (scaledEnemy != null)
                Debug.Log($"[Combat] Scaling OK — {scaledEnemy.name} ×{scaledEnemy.multiplier:F1}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Combat] Scaling échoué, fallback stats base : {e.Message}");
        }

        // 3. Fallback sur stats de base si tout échoue
        gm.CurrentEnemy = scaledEnemy ?? baseEnemy;

        if (gm.CurrentEnemy == null)
        {
            Debug.LogError($"[Combat] CurrentEnemy null — enemyId={enemyId} introuvable. " +
                           $"Vérifie l'Inspector (enemyId doit correspondre à un ID en BDD).");
            _entering = false;
            return;
        }

        Debug.Log($"[Combat] Chargement scène — ennemi : {gm.CurrentEnemy.name}, " +
                  $"type : {gm.CurrentEnemy.type}");

        SceneManager.LoadScene(
            gm.CurrentEnemy.type == "boss" ? "BossCombatScene" : "CombatScene"
        );
    }
}