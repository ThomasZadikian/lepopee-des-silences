using System.Collections.Generic;
using UnityEngine;

namespace RPG.World
{
    // MarkovStateMachine — gère les états d'un ennemi sur la carte mondiale.
    // États possibles : REPOS, PATROUILLE, CHASSE, FUITE
    // Les transitions sont probabilistes (matrice de Markov).
    // Le backend définit la matrice dans Enemy.TransitionMatrix (non utilisé ici
    // pour éviter la désérialisation JSON complexe côté Unity).
    // On utilise une matrice par défaut selon le type d'ennemi.
    public class MarkovStateMachine : MonoBehaviour
    {
        // ── État courant ───────────────────────────────────────────
        public string CurrentState { get; private set; } = EnemyState.Repos;

        // ── Configuration ──────────────────────────────────────────
        [SerializeField] private string enemyType = "basic"; // basic, miniboss, boss
        [SerializeField] private float tickInterval = 3f;   // Secondes entre chaque transition

        // ── Vitesses de déplacement selon état ────────────────────
        [SerializeField] private float speedRepos = 0f;
        [SerializeField] private float speedPatrouille = 1.5f;
        [SerializeField] private float speedChasse = 3.5f;
        [SerializeField] private float speedFuite = 4f;

        private float _tickTimer = 0f;

        // Accès rapide à la vitesse courante
        public float CurrentSpeed => CurrentState switch
        {
            EnemyState.Repos => speedRepos,
            EnemyState.Patrouille => speedPatrouille,
            EnemyState.Chasse => speedChasse,
            EnemyState.Fuite => speedFuite,
            _ => speedPatrouille,
        };

        // ── Matrices de transition par défaut ─────────────────────
        // Format : dict[état courant] → dict[état suivant → probabilité]
        // Somme des probabilités = 1.0
        private readonly Dictionary<string, Dictionary<string, float>> _matrixBasic =
            new Dictionary<string, Dictionary<string, float>>
            {
                [EnemyState.Repos] = new Dictionary<string, float>
                {
                    [EnemyState.Repos] = 0.6f,
                    [EnemyState.Patrouille] = 0.3f,
                    [EnemyState.Chasse] = 0.1f,
                    [EnemyState.Fuite] = 0.0f,
                },
                [EnemyState.Patrouille] = new Dictionary<string, float>
                {
                    [EnemyState.Repos] = 0.2f,
                    [EnemyState.Patrouille] = 0.5f,
                    [EnemyState.Chasse] = 0.3f,
                    [EnemyState.Fuite] = 0.0f,
                },
                [EnemyState.Chasse] = new Dictionary<string, float>
                {
                    [EnemyState.Repos] = 0.0f,
                    [EnemyState.Patrouille] = 0.1f,
                    [EnemyState.Chasse] = 0.8f,
                    [EnemyState.Fuite] = 0.1f,
                },
                [EnemyState.Fuite] = new Dictionary<string, float>
                {
                    [EnemyState.Repos] = 0.5f,
                    [EnemyState.Patrouille] = 0.4f,
                    [EnemyState.Chasse] = 0.0f,
                    [EnemyState.Fuite] = 0.1f,
                },
            };

        private readonly Dictionary<string, Dictionary<string, float>> _matrixBoss =
            new Dictionary<string, Dictionary<string, float>>
            {
                [EnemyState.Repos] = new Dictionary<string, float>
                {
                    [EnemyState.Repos] = 0.3f,
                    [EnemyState.Patrouille] = 0.3f,
                    [EnemyState.Chasse] = 0.4f,
                    [EnemyState.Fuite] = 0.0f,
                },
                [EnemyState.Patrouille] = new Dictionary<string, float>
                {
                    [EnemyState.Repos] = 0.1f,
                    [EnemyState.Patrouille] = 0.3f,
                    [EnemyState.Chasse] = 0.6f,
                    [EnemyState.Fuite] = 0.0f,
                },
                [EnemyState.Chasse] = new Dictionary<string, float>
                {
                    [EnemyState.Repos] = 0.0f,
                    [EnemyState.Patrouille] = 0.0f,
                    [EnemyState.Chasse] = 0.9f,
                    [EnemyState.Fuite] = 0.1f,
                },
                [EnemyState.Fuite] = new Dictionary<string, float>
                {
                    [EnemyState.Repos] = 0.2f,
                    [EnemyState.Patrouille] = 0.2f,
                    [EnemyState.Chasse] = 0.5f,
                    [EnemyState.Fuite] = 0.1f,
                },
            };

        // ── Cycle de tick ──────────────────────────────────────────
        private void Update()
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer < tickInterval) return;
            _tickTimer = 0f;
            Tick();
        }

        // Tire le prochain état selon la matrice de transition
        private void Tick()
        {
            var matrix = enemyType == "boss" ? _matrixBoss : _matrixBasic;

            if (!matrix.TryGetValue(CurrentState, out var transitions))
                return;

            float roll = Random.value; // [0, 1)
            float cumul = 0f;

            foreach (var kv in transitions)
            {
                cumul += kv.Value;
                if (roll < cumul)
                {
                    TransitionTo(kv.Key);
                    return;
                }
            }
        }

        // Transition forcée (ex: joueur détecté)
        public void ForceState(string state)
        {
            TransitionTo(state);
            _tickTimer = 0f; // Reset du tick pour ne pas transitionner immédiatement
        }

        private void TransitionTo(string nextState)
        {
            if (nextState == CurrentState) return;
            Debug.Log($"[Markov] {gameObject.name} : {CurrentState} → {nextState}");
            CurrentState = nextState;
            OnStateChanged(nextState);
        }

        // Surcharger dans une sous-classe pour des effets visuels
        protected virtual void OnStateChanged(string newState) { }
    }

    // Constantes d'état (correspondance avec Constants.cs backend)
    public static class EnemyState
    {
        public const string Repos = "REPOS";
        public const string Patrouille = "PATROUILLE";
        public const string Chasse = "CHASSE";
        public const string Fuite = "FUITE";
    }
}