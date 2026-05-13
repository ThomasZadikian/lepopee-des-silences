using RPG.Core; 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ATBManager : MonoBehaviour
{
    public static ATBManager Instance { get; private set; }

    // ── Etat du combat ─────────────────────────────────────────────
    public enum CombatState { Filling, PlayerChoosing, EnemyActing, CombatOver }
    public CombatState State { get; set; } = CombatState.Filling;

    // ── Combattants ────────────────────────────────────────────────
    public List<Combatant> Combatants { get; private set; } = new List<Combatant>();
    public Queue<Combatant> ActionQueue { get; private set; } = new Queue<Combatant>();
    public Combatant CurrentActor { get; private set; }

    // ── Vitesse de remplissage ATB ─────────────────────────────────
    [SerializeField] private float atbFillRate = 10f; // Multiplicateur global

    private CombatSystem _combatSystem;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Initialize(List<Combatant> combatants, CombatSystem combatSystem)
    {
        Combatants = combatants;
        _combatSystem = combatSystem;
        State = CombatState.Filling;
        ActionQueue.Clear();
    }

    private void Update()
    {
        if (State != CombatState.Filling) return;

        // Remplir les jauges ATB de tous les combattants
        foreach (var c in Combatants)
        {
            Debug.Log($"[ATB] {c.name} ATB: {c.atbCurrent} Speed: {c.speed}");

            if (c.IsDead) continue;
            c.atbCurrent += c.speed * atbFillRate * Time.deltaTime;

            if (c.IsATBFull && !ActionQueue.Contains(c))
            {
                c.atbCurrent = 100f;
                ActionQueue.Enqueue(c);
            }
        }

        // Traiter la file d'action
        if (ActionQueue.Count > 0 && CurrentActor == null)
            ProcessNextActor();
    }

    private void ProcessNextActor()
    {
        CurrentActor = ActionQueue.Dequeue();

        if (CurrentActor.isPlayer)
        {
            // Figer le temps — joueur choisit son action
            State = CombatState.PlayerChoosing;
            CombatUIManager.Instance.ShowActionButtons(true);
        }
        else
        {
            // Ennemi agit automatiquement
            State = CombatState.EnemyActing;
            EnemyActAsync();
        }
    }

    private async void EnemyActAsync()
    {
        await Task.Delay(800);

        var player = Combatants.Find(c => c.isPlayer);
        if (player != null && !player.IsDead)
        {
            var currentActor = CurrentActor;
            int dmg = Mathf.Max(1,
                (int)(currentActor.speed * 2) - (GameManager.Instance.Player.Strength / 4));
            dmg = Mathf.Max(1, dmg + UnityEngine.Random.Range(-dmg / 5, dmg / 5));

            player.currentHP = Mathf.Max(0, player.currentHP - dmg);
            GameManager.Instance.TakeDamage(dmg);

            CombatUIManager.Instance.AddLog(
                $"{currentActor.name} attaque et inflige {dmg} degats !");
            CombatUIManager.Instance.UpdateBars();

            if (player.IsDead)
            {
                State = CombatState.CombatOver;
                await CombatUIManager.Instance.EndCombat(false);
                return;
            }
        }

        FinishTurn();
    }

    public void PlayerAttack()
    {
        if (State != CombatState.PlayerChoosing) return;

        var enemy = Combatants.Find(c => !c.isPlayer && !c.IsDead);
        if (enemy == null) return;

        var player = Combatants.Find(c => c.isPlayer);
        var enemyResponse = GameManager.Instance.CurrentEnemy;
        int dmg = Mathf.RoundToInt(
            GameManager.Instance.Player.Strength * 2 *
            (enemyResponse != null ? enemyResponse.physicalResistance : 1f));
        dmg = Mathf.Max(1, dmg + UnityEngine.Random.Range(-dmg / 5, dmg / 5)); dmg = Mathf.Max(1, dmg + UnityEngine.Random.Range(-dmg / 5, dmg / 5));

        enemy.currentHP = Mathf.Max(0, enemy.currentHP - dmg);
        CombatUIManager.Instance.AddLog($"Tu attaques et infliges {dmg} degats !");
        CombatUIManager.Instance.UpdateBars();

        if (enemy.IsDead)
        {
            State = CombatState.CombatOver;
            CombatUIManager.Instance.EnemyDefeated(enemy);
            return;
        }

        FinishTurn();
    }

    public void FinishTurn()
    {
        // Remettre la jauge ATB à 0
        if (CurrentActor != null)
            CurrentActor.atbCurrent = 0f;

        CurrentActor = null;
        CombatUIManager.Instance.ShowActionButtons(false);
        State = CombatState.Filling;
    }
}