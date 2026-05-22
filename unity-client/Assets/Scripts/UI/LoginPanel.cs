using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using RPG.Core;
using RPG.Services;

// LoginPanel gère le flux complet :
// Login → MFA Setup (QR code) → MFA Verify → Jeu
// Login → MFA Verify (compte existant) → Jeu
public class LoginPanel : MonoBehaviour
{
    // ── Panels ────────────────────────────────────────────────────
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject mfaSetupPanel;
    [SerializeField] private GameObject mfaVerifyPanel;

    // ── Login ─────────────────────────────────────────────────────
    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private Button loginButton;
    [SerializeField] private TMP_Text loginError;

    // ── MFA Setup ─────────────────────────────────────────────────
    // Affiche le QR code sous forme d'URL texte (pas de QR visuel en Unity)
    [SerializeField] private TMP_Text setupQrUrl;
    [SerializeField] private TMP_Text setupManualKey;
    [SerializeField] private TMP_InputField setupCodeField;
    [SerializeField] private Button setupConfirmButton;
    [SerializeField] private TMP_Text setupError;

    // ── MFA Verify ────────────────────────────────────────────────
    [SerializeField] private TMP_InputField verifyCodeField;
    [SerializeField] private Button verifyButton;
    [SerializeField] private TMP_Text verifyError;

    private void Start()
    {
        ShowPanel(loginPanel);
    }

    // ═══════════════════════════════════════════════════════════════
    // LOGIN
    // ═══════════════════════════════════════════════════════════════
    public async void OnLoginClicked()
    {
        SetInteractable(loginButton, false);
        loginError.text = "";

        var result = await AuthService.Instance.LoginAsync(
            usernameField.text.Trim(),
            passwordField.text
        );

        switch (result.State)
        {
            case AuthState.NeedsMfaSetup:
                await ShowMfaSetup();
                break;

            case AuthState.NeedsMfaVerify:
                ShowPanel(mfaVerifyPanel);
                verifyError.text = "";
                break;

            case AuthState.Success:
                await LoadGameAsync();
                break;

            case AuthState.Failed:
            default:
                loginError.text = result.Message ?? "Identifiants incorrects.";
                SetInteractable(loginButton, true);
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // MFA SETUP — affiche le QR code (URL texte) + clé manuelle
    // ═══════════════════════════════════════════════════════════════
    private async System.Threading.Tasks.Task ShowMfaSetup()
    {
        var setup = await AuthService.Instance.SetupMfaAsync();
        if (setup == null || !setup.success)
        {
            loginError.text = "Erreur lors de la configuration MFA.";
            SetInteractable(loginButton, true);
            return;
        }

        if (setupQrUrl != null) setupQrUrl.text = setup.qrCodeUri;
        if (setupManualKey != null) setupManualKey.text = setup.manualKey;

        ShowPanel(mfaSetupPanel);
        setupError.text = "";
    }

    public async void OnSetupConfirmClicked()
    {
        SetInteractable(setupConfirmButton, false);
        setupError.text = "";

        string code = setupCodeField.text.Trim();
        if (code.Length != 6)
        {
            setupError.text = "Le code doit contenir 6 chiffres.";
            SetInteractable(setupConfirmButton, true);
            return;
        }

        var result = await AuthService.Instance.VerifyMfaSetupAsync(code);
        if (result.State == AuthState.Success)
        {
            await LoadGameAsync();
        }
        else
        {
            setupError.text = result.Message ?? "Code incorrect.";
            SetInteractable(setupConfirmButton, true);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // MFA VERIFY — connexion avec compte existant
    // ═══════════════════════════════════════════════════════════════
    public async void OnVerifyClicked()
    {
        SetInteractable(verifyButton, false);
        verifyError.text = "";

        string code = verifyCodeField.text.Trim();
        if (code.Length != 6)
        {
            verifyError.text = "Le code doit contenir 6 chiffres.";
            SetInteractable(verifyButton, true);
            return;
        }

        var result = await AuthService.Instance.LoginWithMfaAsync(code);
        if (result.State == AuthState.Success)
        {
            await LoadGameAsync();
        }
        else
        {
            verifyError.text = result.Message ?? "Code incorrect.";
            SetInteractable(verifyButton, true);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Chargement du jeu après auth réussie
    // ═══════════════════════════════════════════════════════════════
    private async System.Threading.Tasks.Task LoadGameAsync()
    {
        bool loaded = await PlayerService.Instance.LoadProfileAsync();
        if (!loaded)
        {
            loginError.text = "Profil introuvable.";
            ShowPanel(loginPanel);
            SetInteractable(loginButton, true);
            return;
        }

        // Chargement parallèle ennemis + compétences
        var enemyTask = EnemyService.Instance.LoadEnemiesAsync();
        var skillTask = SkillService.Instance != null
            ? SkillService.Instance.LoadPlayerSkillsAsync()
            : System.Threading.Tasks.Task.FromResult(false);

        await System.Threading.Tasks.Task.WhenAll(enemyTask, skillTask);

        // Restaurer la dernière position
        var lastSave = await PlayerService.Instance.LoadLastSaveAsync();
        if (lastSave != null)
        {
            GameManager.Instance.PosX = lastSave.positionX;
            GameManager.Instance.PosY = lastSave.positionY;
            GameManager.Instance.CurrentZone = lastSave.currentZone;
        }

        SceneManager.LoadScene("GameScene");
    }

    // ── Helpers ───────────────────────────────────────────────────
    private void ShowPanel(GameObject panel)
    {
        if (loginPanel != null) loginPanel.SetActive(panel == loginPanel);
        if (mfaSetupPanel != null) mfaSetupPanel.SetActive(panel == mfaSetupPanel);
        if (mfaVerifyPanel != null) mfaVerifyPanel.SetActive(panel == mfaVerifyPanel);
    }

    private void SetInteractable(Button btn, bool state)
    {
        if (btn != null) btn.interactable = state;
    }
}