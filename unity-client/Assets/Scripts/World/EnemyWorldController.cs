using RPG.World;
using UnityEngine;

// EnemyWorldController — combine Markov + InfluenceDetector + mouvement.
// Remplace CombatTrigger pour les ennemis avec IA de carte.
// À placer sur chaque ennemi de la GameScene.
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

    private void Start()
    {
        // Désactiver si ennemi déjà mort
        if (RPG.Core.GameManager.Instance.DeadEnemies.Contains(instanceId))
        {
            gameObject.SetActive(false);
            return;
        }

        // Configurer le rayon d'influence depuis le backend
        var enemy = EnemyService.Instance?.GetEnemyById(enemyId);
        if (enemy != null)
            _detector.SetRadius(enemy.influenceRadius);

        _immunityTimer = immunityDuration;

        // Trouver le joueur
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
            case EnemyState.Repos:
                _rb.linearVelocity = Vector2.zero;
                break;

            case EnemyState.Patrouille:
                Patrol();
                break;

            case EnemyState.Chasse:
                ChasePlayer();
                break;

            case EnemyState.Fuite:
                FleeFromPlayer();
                break;
        }
    }

    // ── Patrouille entre des points définis ───────────────────────
    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        if (_waitingAtPoint)
        {
            _patrolWait -= Time.fixedDeltaTime;
            if (_patrolWait <= 0f)
            {
                _waitingAtPoint = false;
                _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            }
            return;
        }

        var target = patrolPoints[_patrolIndex].position;
        var dir = (target - transform.position);

        if (dir.magnitude < 0.15f)
        {
            _waitingAtPoint = true;
            _patrolWait = patrolWaitTime;
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        _rb.linearVelocity = dir.normalized * _markov.CurrentSpeed;
    }

    // ── Chasse le joueur ──────────────────────────────────────────
    private void ChasePlayer()
    {
        if (_playerTransform == null) return;
        var dir = (_playerTransform.position - transform.position).normalized;
        _rb.linearVelocity = new Vector2(dir.x, dir.y) * _markov.CurrentSpeed;
    }

    // ── Fuite — s'éloigne du joueur ───────────────────────────────
    private void FleeFromPlayer()
    {
        if (_playerTransform == null) return;
        var dir = (transform.position - _playerTransform.position).normalized;
        _rb.linearVelocity = new Vector2(dir.x, dir.y) * _markov.CurrentSpeed;
    }

    // ── Contact physique → déclanche le combat ────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_entering) return;
        if (_immunityTimer > 0f) return;
        if (!other.CompareTag("Player")) return;

        // Ignorer si on est en fuite
        if (_markov.CurrentState == EnemyState.Fuite) return;

        _entering = true;

        var pos = other.transform.position;
        RPG.Core.GameManager.Instance.PosX = pos.x;
        RPG.Core.GameManager.Instance.PosY = pos.y;
        RPG.Core.GameManager.Instance.CurrentEnemyInstanceId = instanceId;

        _ = StartCombatAsync();
    }

    private async System.Threading.Tasks.Task StartCombatAsync()
    {
        var gm = RPG.Core.GameManager.Instance;
        var baseEnemy = EnemyService.Instance.GetEnemyById(enemyId);
        if (baseEnemy == null) { _entering = false; return; }

        EnemyResponse scaledEnemy = null;
        try
        {
            int level = gm.Player.Level;
            var wrapper = await RPG.Network.ApiClient.Instance
                .GetAsync<ScaledEnemyWrapper>(
                    $"/api/enemies/{enemyId}/scaled?playerLevel={level}&equipmentBonus=0"
                );
            scaledEnemy = wrapper?.enemy;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EnemyWorld] Scaling échoué, fallback stats base : {e.Message}");
        }

        gm.CurrentEnemy = scaledEnemy ?? baseEnemy;

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            baseEnemy.type == "boss" ? "BossCombatScene" : "CombatScene"
        );
    }
}