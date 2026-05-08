using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using RPG.Core; 
using RPG.Services;

public class LoginPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private UnityEngine.UI.Button loginButton;
    [SerializeField] private TMP_Text errorLabel;

    public async void OnLoginClicked()
    {
        loginButton.interactable = false;
        errorLabel.text = "";

        string token = await AuthService.Instance.LoginAsync(
            usernameField.text.Trim(),
            passwordField.text
        );

        if (token == null)
        {
            errorLabel.text = "Identifiants incorrects.";
            loginButton.interactable = true;
            return;
        }

        bool loaded = await PlayerService.Instance.LoadProfileAsync();
        if (!loaded)
        {
            errorLabel.text = "Profil introuvable.";
            loginButton.interactable = true;
            return;
        }

        var lastSave = await PlayerService.Instance.LoadLastSaveAsync();
        if (lastSave != null)
        {
            GameManager.Instance.PosX = lastSave.positionX;
            GameManager.Instance.PosY = lastSave.positionY;
            GameManager.Instance.CurrentZone = lastSave.currentZone;
        }

        SceneManager.LoadScene("GameScene");
    }

    private void ShowError(string message)
    {
        errorLabel.text = message;
    }
}