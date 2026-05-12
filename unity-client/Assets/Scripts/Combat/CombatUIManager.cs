using RPG.Core;
using RPG.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class CombatUIManager : MonoBehaviour
{
    public static CombatUIManager Instance { get; private set; }

    [Header("Combattants")]
    [SerializeField] private RectTransform playerSpriteTransform;
    [SerializeField] private RectTransform enemySpriteTransform;

    [Header("Barres joueur")]
    [SerializeField] private Slider playerHPBar;
    [SerializeField] private Slider playerMPBar;
    [SerializeField] private Slider playerATBBar;
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text playerHPText;

    [Header("Barres ennemi")]
    [SerializeField] private Slider enemyHPBar;
    [SerializeField] private Slider enemyATBBar;
    [SerializeField] private TMP_Text enemyName;
    [SerializeField] private TMP_Text enemyHPText;

    [Header("Actions")]
    [SerializeField] private GameObject actionPanel;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button fleeButton;

    [Header("Log")]
    [SerializeField] private TMP_Text combatLog;

    [Header("File d'initiative")]
    [SerializeField] private Transform initiativeContainer;
    [SerializeField] private GameObject initiativePortraitPrefab;

    private List<Combatant> _combatants;
    private Combatant _player;
    private Combatant _enemy;
    private Vector3 _playerBasePos;
    private Vector3 _enemyBasePos;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Initialize(List<Combatant> combatants)
    {
        _combatants = combatants;
        _player = combatants.Find(c => c.isPlayer);
        _enemy = combatants.Find(c => !c.isPlayer);

        if (playerSpriteTransform != null) _playerBasePos = playerSpriteTransform.anchoredPosition;
        if (enemySpriteTransform != null) _enemyBasePos = enemySpriteTransform.anchoredPosition;

        if (playerName != null) playerName.text = _player?.name ?? "Joueur";
        if (enemyName != null) enemyName.text = _enemy?.name ?? "Ennemi";

        ShowActionButtons(false);
        UpdateBars();
    }

    private void Update()
    {
        if (_player != null && playerATBBar != null)
            playerATBBar.value = _player.atbCurrent / 100f;

        if (_enemy != null && enemyATBBar != null)
            enemyATBBar.value = _enemy.atbCurrent / 100f;

        UpdateInitiativeDisplay();
    }

    public void UpdateBars()
    {
        if (_player != null)
        {
            if (playerHPBar != null) playerHPBar.value = _player.maxHP > 0 ? (float)_player.currentHP / _player.maxHP : 0;
            if (playerHPText != null) playerHPText.text = $"{_player.currentHP} / {_player.maxHP}";
        }
        if (_enemy != null)
        {
            if (enemyHPBar != null) enemyHPBar.value = _enemy.maxHP > 0 ? (float)_enemy.currentHP / _enemy.maxHP : 0;
            if (enemyHPText != null) enemyHPText.text = $"{_enemy.currentHP} / {_enemy.maxHP}";
        }
    }

    public void ShowActionButtons(bool show)
    {
        if (actionPanel != null) actionPanel.SetActive(show);
    }

    public void AddLog(string message)
    {
        if (combatLog != null)
            combatLog.text += $"\n{message}";
    }

    private void UpdateInitiativeDisplay()
    {
        if (initiativeContainer == null || initiativePortraitPrefab == null) return;

        foreach (Transform child in initiativeContainer)
            Destroy(child.gameObject);

        if (_combatants == null) return;

        var simATB = new Dictionary<Combatant, float>();
        foreach (var c in _combatants)
            simATB[c] = c.atbCurrent;

        var upcomingTurns = new List<Combatant>();
        int maxIterations = 50;

        while (upcomingTurns.Count < 6 && maxIterations-- > 0)
        {
            float minTime = float.MaxValue;
            foreach (var c in _combatants)
            {
                if (c.IsDead) continue;
                float timeToFull = (100f - simATB[c]) / (c.speed * 0.5f);
                if (timeToFull < minTime) minTime = timeToFull;
            }

            foreach (var c in _combatants)
                simATB[c] += c.speed * 0.5f * minTime;

            Combatant next = null;
            float maxATB = 0f;
            foreach (var c in _combatants)
            {
                if (c.IsDead) continue;
                if (simATB[c] >= 100f && simATB[c] > maxATB)
                {
                    maxATB = simATB[c];
                    next = c;
                }
            }

            if (next != null)
            {
                upcomingTurns.Add(next);
                simATB[next] = 0f;
            }
        }

        foreach (var c in upcomingTurns)
        {
            var portrait = Instantiate(initiativePortraitPrefab, initiativeContainer);
            var img = portrait.GetComponent<Image>();
            if (img != null)
                img.color = c.isPlayer ? Color.white : Color.red;

            var txt = portrait.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.text = c.name[0].ToString();
        }
    }

    public async Task AnimateAttack(bool isPlayer)
    {
        var t = isPlayer ? playerSpriteTransform : enemySpriteTransform;
        var basePos = isPlayer ? _playerBasePos : _enemyBasePos;
        if (t == null) return;

        float direction = isPlayer ? 1f : -1f;
        Vector3 target = basePos + new Vector3(direction * 60f, 0f, 0f);

        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            t.anchoredPosition = Vector3.Lerp(basePos, target, elapsed / 0.15f);
            elapsed += Time.deltaTime;
            await Task.Yield();
        }

        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            t.anchoredPosition = Vector3.Lerp(target, basePos, elapsed / 0.15f);
            elapsed += Time.deltaTime;
            await Task.Yield();
        }

        t.anchoredPosition = basePos;
    }

    public async Task AnimateFlee()
    {
        if (playerSpriteTransform == null) return;

        Vector3 target = _playerBasePos + new Vector3(300f, 0f, 0f);
        float elapsed = 0f;

        while (elapsed < 0.5f)
        {
            playerSpriteTransform.anchoredPosition = Vector3.Lerp(
                _playerBasePos, target, elapsed / 0.5f);
            elapsed += Time.deltaTime;
            await Task.Yield();
        }
    }

    public async Task EndCombat(bool playerWon)
    {
        ShowActionButtons(false);
        AddLog(playerWon ? "Victoire !" : "Defaite... Chargement de la derniere sauvegarde.");
        await Task.Delay(2000);

        if (!playerWon)
        {
            await PlayerService.Instance.LoadProfileAsync();

            var lastSave = await PlayerService.Instance.LoadLastSaveAsync();
            if (lastSave != null)
            {
                GameManager.Instance.PosX = lastSave.positionX;
                GameManager.Instance.PosY = lastSave.positionY;
                GameManager.Instance.CurrentZone = lastSave.currentZone;
            }
        }

        SceneManager.LoadScene("GameScene");
    }

    public async void EnemyDefeated(Combatant enemy)
    {
        var p = GameManager.Instance.Player;
        var e = GameManager.Instance.CurrentEnemy;
        if (e != null)
        {
            p.Experience += e.experienceReward;
            p.Gold += e.goldReward;
        }

        GameManager.Instance.RegisterDeadEnemy(GameManager.Instance.CurrentEnemyInstanceId);
        AddLog($"{enemy.name} est vaincu ! +{e?.experienceReward} XP, +{e?.goldReward} Gold");

        await Task.Delay(2000);
        SceneManager.LoadScene("GameScene");
    }

    public void OnAttackClicked()
    {
        if (ATBManager.Instance.State != ATBManager.CombatState.PlayerChoosing) return;
        _ = AnimateAttack(true);
        ATBManager.Instance.PlayerAttack();
    }

    public async void OnFleeClicked()
    {
        if (ATBManager.Instance.State != ATBManager.CombatState.PlayerChoosing) return;
        ATBManager.Instance.State = ATBManager.CombatState.CombatOver;
        AddLog("Tu prends la fuite !");
        await AnimateFlee();
        await Task.Delay(500);
        SceneManager.LoadScene("GameScene");
    }
}