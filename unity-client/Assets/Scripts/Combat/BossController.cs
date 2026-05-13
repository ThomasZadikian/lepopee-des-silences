using RPG.Core;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Image = UnityEngine.UI.Image; 

public class BossController : MonoBehaviour
{
    public static BossController Instance { get; private set; }
    public bool FireballUsedThisCycle { get; private set; } = false;


    [Header("Boss Config")]
    [SerializeField] private int bossEnemyId = 17; // Dragon de l'Aube

    [Header("References")]
    [SerializeField] private Image bossSprite;
    [SerializeField] private Image bossAura;        // Image derriere le sprite — aura dorée
    [SerializeField] private RectTransform bossSpriteTransform;
    [SerializeField] private float breathSpeed = 1.5f;  // Vitesse de respiration
    [SerializeField] private float breathAmount = 0.05f; // Amplitude (5%)

    // ── Etat du boss ───────────────────────────────────────────────
    private bool _phase2Triggered = false; // 75% HP
    private bool _phase3Triggered = false; // 50% HP
    private bool _introPlayed = false;

    private Combatant _bossCombatant;
    private Vector3 _baseSpriteScale;


    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (bossSpriteTransform != null)
            _baseSpriteScale = bossSpriteTransform.localScale;
    }

    private void Update()
    {
        if (bossSpriteTransform == null) return;

        // Oscillation sinusoïdale — grandit et rapetisse en boucle
        float scale = 1f + Mathf.Sin(Time.time * breathSpeed) * breathAmount;
        bossSpriteTransform.localScale = _baseSpriteScale * scale;
    }


    public bool IsBossFight => GameManager.Instance.CurrentEnemy?.id == bossEnemyId;

    public void Initialize(Combatant bossCombatant)
    {
        _bossCombatant = bossCombatant;
        _phase2Triggered = false;
        _phase3Triggered = false;
        _introPlayed = false;

        if (bossAura != null)
            bossAura.gameObject.SetActive(false);

        if (IsBossFight)
            _ = PlayIntroAsync();
    }

    // ── Intro — 2 sauts ───────────────────────────────────────────
    public async Task PlayIntroAsync()
    {
        if (_introPlayed) return;
        _introPlayed = true;

        CombatUIManager.Instance.AddLog("Le Dragon de l'Aube rugit !");
        await JumpAsync();
        await Task.Delay(300);
        await JumpAsync();
    }

    // ── Animation saut ────────────────────────────────────────────
    private async Task JumpAsync()
    {
        if (bossSpriteTransform == null) return;

        var basePos = bossSpriteTransform.anchoredPosition;
        var jumpPos = basePos + new Vector2(0f, 60f);

        // Monter
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            bossSpriteTransform.anchoredPosition = Vector3.Lerp(basePos, jumpPos, elapsed / 0.2f);
            elapsed += Time.deltaTime;
            await Task.Yield();
        }

        // Descendre
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            bossSpriteTransform.anchoredPosition = Vector3.Lerp(jumpPos, basePos, elapsed / 0.2f);
            elapsed += Time.deltaTime;
            await Task.Yield();
        }

        bossSpriteTransform.anchoredPosition = basePos;
    }

    // ── Vérifier les phases selon les HP ──────────────────────────
    public async Task CheckPhases()
    {
        if (_bossCombatant == null) return;

        float hpPercent = (float)_bossCombatant.currentHP / _bossCombatant.maxHP;

        // Phase 2 — 75% HP
        if (!_phase2Triggered && hpPercent <= 0.75f)
        {
            _phase2Triggered = true;
            await TriggerPhase2();
        }

        // Phase 3 — 50% HP
        if (!_phase3Triggered && hpPercent <= 0.5f)
        {
            _phase3Triggered = true;
            await TriggerPhase3();
        }
    }

    // ── Phase 2 — 75% HP ──────────────────────────────────────────
    private async Task TriggerPhase2()
    {
        CombatUIManager.Instance.AddLog("Le Dragon devient écarlate !");

        // Teinter le sprite en rouge
        if (bossSprite != null)
            bossSprite.color = new Color(1f, 0.4f, 0.4f, 1f);

        // Sauts
        await JumpAsync();
        await Task.Delay(200);
        await JumpAsync();

        // Augmenter la vitesse ATB de 25%
        _bossCombatant.speed *= 1.25f;
        CombatUIManager.Instance.AddLog("Sa vitesse augmente !");
    }

    // ── Phase 3 — 50% HP ──────────────────────────────────────────
    private async Task TriggerPhase3()
    {
        CombatUIManager.Instance.AddLog("Le Dragon souffle une flamme dévasta trice !");

        // Souffle de feu — 25% des HP restants du joueur
        var player = GameManager.Instance.Player;
        int dmg = Mathf.Max(1, player.CurrentHP / 4);
        GameManager.Instance.TakeDamage(dmg);

        // Mettre à jour le Combatant joueur
        var playerCombatant = ATBManager.Instance.Combatants.Find(c => c.isPlayer);
        if (playerCombatant != null)
            playerCombatant.currentHP = player.CurrentHP;

        CombatUIManager.Instance.AddLog($"Le souffle inflige {dmg} degats !");
        CombatUIManager.Instance.UpdateBars();

        // Augmenter vitesse ATB de 25% supplémentaires (total +50%)
        _bossCombatant.speed *= 1.25f;

        // Augmenter défense (résistance physique simulée)
        _bossCombatant.physicalResistance *= 0.85f; // -15% dégâts reçus

        // Augmenter force
        _bossCombatant.strength = Mathf.RoundToInt(_bossCombatant.strength * 1.1f);

        // Aura dorée
        if (bossAura != null)
        {
            bossAura.gameObject.SetActive(true);
            bossAura.color = new Color(1f, 0.85f, 0f, 0.4f);
        }

        CombatUIManager.Instance.AddLog("Une aura dorée enveloppe le Dragon !");
    }

    // ── Boule de feu à demi-tour (ATB entre 40 et 60%) ───────────
    public bool ShouldCastFireball(float atbPercent)
    {
        if (!IsBossFight) return false;
        if (_phase2Triggered) return false; // Phase 2 : plus de boule de feu
        return atbPercent >= 0.4f && atbPercent <= 0.6f;
    }

    public int GetFireballDamage()
    {
        // Boule de feu à 1/4 des dégâts normaux
        // BaseDamage de Boule de Feu = 35
        return Mathf.Max(1, 35 / 4);
    }

    // Appelé par ATBManager quand ATB atteint 50%
    public void CastIntermediateFireball(Combatant bossC)
    {
        if (_phase2Triggered) return; // Phase 2 : plus de boule de feu
        FireballUsedThisCycle = true;
        _ = FireballAsync();
    }

    // Reset à chaque nouveau cycle ATB (appelé dans FinishTurn)
    public void ResetFireballCycle()
    {
        FireballUsedThisCycle = false;
    }

    private async Task FireballAsync()
    {
        await Task.Delay(300);
        var player = ATBManager.Instance.Combatants.Find(c => c.isPlayer);
        if (player == null || player.IsDead) return;

        int dmg = Mathf.Max(1, 35 / 4); // Boule de feu à 1/4 des dégâts
        player.currentHP = Mathf.Max(0, player.currentHP - dmg);
        GameManager.Instance.TakeDamage(dmg);

        CombatUIManager.Instance.AddLog($"Le Dragon crache une flamme et inflige {dmg} degats !");
        CombatUIManager.Instance.UpdateBars();

        if (player.IsDead)
        {
            ATBManager.Instance.State = ATBManager.CombatState.CombatOver;
            await CombatUIManager.Instance.EndCombat(false);
        }
    }
}