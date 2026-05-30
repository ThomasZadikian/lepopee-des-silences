using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Core;
using RPG.Network.Dto;
using Image = UnityEngine.UI.Image;

public class CombatUI : MonoBehaviour
{
    [Header("Enemy")]
    [SerializeField] private Image enemySprite;
    [SerializeField] private TMP_Text enemyName;
    [SerializeField] private Slider enemyHPBar;

    [Header("Player")]
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private Slider playerHPBar;
    [SerializeField] private Slider playerMPBar;

    [Header("Actions")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button spell1Button;
    [SerializeField] private Button spell2Button;
    [SerializeField] private Button spell3Button;
    [SerializeField] private Button potionButton;
    [SerializeField] private Button fleeButton;

    [Header("Log")]
    [SerializeField] private TMP_Text combatLog;

    private CombatSystem _combatSystem;
    private EnemyResponse _currentEnemy;
    private bool _playerTurn = true;

    private void Awake()
    {
        _combatSystem = GetComponent<CombatSystem>();
    }

    // ── Initialisation du combat ───────────────────────────────────────────────
    public void StartCombat(EnemyResponse enemy)
    {
        _currentEnemy = enemy;
        _playerTurn = true;

        _combatSystem.StartCombat(enemy);

        // Afficher les infos
        enemyName.text = enemy.name;
        playerName.text = GameManager.Instance.Player.CharacterName;

        UpdateBars();
        SetButtonsInteractable(true);
        AddLog($"Un {enemy.name} apparaît !");
    }

    // ── Actions du joueur ──────────────────────────────────────────────────────
    public async void OnAttackClicked()
    {
        if (!_playerTurn) return;
        SetButtonsInteractable(false);

        int dmg = _combatSystem.PlayerPhysicalAttack();
        AddLog($"Tu attaques et infliges {dmg} dégâts !");
        UpdateBars();

        if (_combatSystem.IsEnemyDead)
        {
            AddLog($"{_currentEnemy.name} est vaincu !");
            await _combatSystem.EndCombatAsync(true);
            EndCombat();
            return;
        }

        await EnemyTurnAsync();
    }

    public async void OnFleeClicked()
    {
        if (!_playerTurn) return;
        AddLog("Tu prends la fuite !");
        await _combatSystem.EndCombatAsync(false);
        EndCombat();
    }

    // ── Tour ennemi ────────────────────────────────────────────────────────────
    private async System.Threading.Tasks.Task EnemyTurnAsync()
    {
        await System.Threading.Tasks.Task.Delay(1000); // Pause 1 seconde

        int dmg = _combatSystem.EnemyAttack();
        AddLog($"{_currentEnemy.name} t'attaque et inflige {dmg} dégâts !");
        UpdateBars();

        if (_combatSystem.IsPlayerDead)
        {
            AddLog("Tu es vaincu...");
            await _combatSystem.EndCombatAsync(false);
            EndCombat();
            return;
        }

        _playerTurn = true;
        SetButtonsInteractable(true);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private void UpdateBars()
    {
        var p = GameManager.Instance.Player;
        playerHPBar.value = (float)p.CurrentHP / p.MaxHP;
        playerMPBar.value = (float)p.CurrentMP / p.MaxMP;
        enemyHPBar.value = (float)_combatSystem.EnemyHP / _currentEnemy.maxHP;
    }

    private void SetButtonsInteractable(bool state)
    {
        attackButton.interactable = state;
        spell1Button.interactable = state;
        spell2Button.interactable = state;
        spell3Button.interactable = state;
        potionButton.interactable = state;
        fleeButton.interactable = state;
    }

    private void AddLog(string message)
    {
        combatLog.text += $"\n{message}";
    }

    private void EndCombat()
    {
        gameObject.SetActive(false); // Cache le CombatCanvas
    }
}