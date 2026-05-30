using RPG.Core;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Image = UnityEngine.UI.Image;

public class CombatManager : MonoBehaviour
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
    [SerializeField] private Button fleeButton;

    [Header("Log")]
    [SerializeField] private TMP_Text combatLog;

    private CombatSystem _combatSystem;
    private EnemyResponse _currentEnemy;
    private bool _playerTurn = true;
    private bool _combatEnded = false;

    private void Start()
    {
        _combatSystem = gameObject.AddComponent<CombatSystem>();
        _currentEnemy = GameManager.Instance.CurrentEnemy;

        if (_currentEnemy == null)
        {
            Debug.LogError("[Combat] Aucun ennemi défini dans GameManager.");
            return;
        }

        _combatSystem.StartCombat(_currentEnemy);

        enemyName.text = _currentEnemy.name;
        playerName.text = GameManager.Instance.Player.CharacterName;

        UpdateBars();
        AddLog($"Un {_currentEnemy.name} apparaît !");
    }

    // ── Attaque joueur ─────────────────────────────────────────────────────────
    public async void OnAttackClicked()
    {
        Debug.Log($"[Combat] OnAttackClicked appelé. PlayerTurn: {_playerTurn}, CombatEnded: {_combatEnded}");
        if (!_playerTurn || _combatEnded) return;
        SetButtonsInteractable(false);
        _playerTurn = false;

        int dmg = _combatSystem.PlayerPhysicalAttack();
        AddLog($"Tu attaques et infliges {dmg} dégâts !");
        UpdateBars();

        if (_combatSystem.IsEnemyDead)
        {
            AddLog($"{_currentEnemy.name} est vaincu ! +{_currentEnemy.experienceReward} XP, +{_currentEnemy.goldReward} Gold");
            GameManager.Instance.RegisterDeadEnemy(GameManager.Instance.CurrentEnemyInstanceId);
            await EndCombat(true);
            return;
        }

        await EnemyTurnAsync();
    }

    // ── Fuite ──────────────────────────────────────────────────────────────────
    public async void OnFleeClicked()
    {
        if (!_playerTurn || _combatEnded) return;
        _combatEnded = true;
        AddLog("Tu prends la fuite !");
        await _combatSystem.EndCombatAsync(false);
        await Task.Delay(1500);
        SceneManager.LoadScene("GameScene");
    }

    // ── Tour ennemi ────────────────────────────────────────────────────────────
    private async Task EnemyTurnAsync()
    {
        await Task.Delay(1000);

        int dmg = _combatSystem.EnemyAttack();
        AddLog($"{_currentEnemy.name} attaque et inflige {dmg} dégâts !");
        UpdateBars();

        if (_combatSystem.IsPlayerDead)
        {
            AddLog("Tu es vaincu...");
            await EndCombat(false);
            return;
        }

        _playerTurn = true;
        SetButtonsInteractable(true);
    }

    // ── Fin du combat ──────────────────────────────────────────────────────────
    private async Task EndCombat(bool playerWon)
    {
        _combatEnded = true;
        SetButtonsInteractable(false);
        await _combatSystem.EndCombatAsync(playerWon);
        await Task.Delay(2000);
        SceneManager.LoadScene("GameScene");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private void UpdateBars()
    {
        var p = GameManager.Instance.Player;
        playerHPBar.value = p.MaxHP > 0 ? (float)p.CurrentHP / p.MaxHP : 0;
        playerMPBar.value = p.MaxMP > 0 ? (float)p.CurrentMP / p.MaxMP : 0;
        enemyHPBar.value = _currentEnemy.maxHP > 0 ? (float)_combatSystem.EnemyHP / _currentEnemy.maxHP : 0;
    }

    private void SetButtonsInteractable(bool state)
    {
        attackButton.interactable = state;
        fleeButton.interactable = state;
    }

    private void AddLog(string message)
    {
        combatLog.text += $"\n{message}";
    }
}