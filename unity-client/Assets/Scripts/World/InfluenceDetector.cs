using RPG.World;
using UnityEngine;

// InfluenceDetector — détecte le joueur dans le rayon d'influence de l'ennemi
// et déclenche les transitions d'état Markov appropriées.
// Doit être placé sur le même GameObject que MarkovStateMachine.
// Le collider doit être réglé en Trigger avec le rayon influenceRadius.
[RequireComponent(typeof(MarkovStateMachine))]
[RequireComponent(typeof(CircleCollider2D))]
public class InfluenceDetector : MonoBehaviour
{
    [SerializeField] private float influenceRadius = 5f;

    private MarkovStateMachine _markov;
    private CircleCollider2D _collider;
    private bool _playerInRange = false;

    private void Awake()
    {
        _markov = GetComponent<MarkovStateMachine>();
        _collider = GetComponent<CircleCollider2D>();

        // Configurer le collider automatiquement
        _collider.isTrigger = true;
        _collider.radius = influenceRadius;
    }

    // Initialise le rayon depuis les données du backend
    public void SetRadius(float radius)
    {
        influenceRadius = radius;
        _collider.radius = radius;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;

        // Le joueur entre dans la zone → passer en CHASSE
        _markov.ForceState(EnemyState.Chasse);
        Debug.Log($"[Influence] {gameObject.name} détecte le joueur → CHASSE");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;

        // Le joueur quitte la zone → repasser en PATROUILLE
        _markov.ForceState(EnemyState.Patrouille);
        Debug.Log($"[Influence] {gameObject.name} perd le joueur → PATROUILLE");
    }

    // Accès à l'état actuel pour le mouvement
    public bool IsChasing => _markov.CurrentState == EnemyState.Chasse;
    public bool IsPlayer => _playerInRange;
}