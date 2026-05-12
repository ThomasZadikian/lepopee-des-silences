using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using RPG.Core;
using RPG.Services;

public class SaveTrigger : MonoBehaviour
{
    [SerializeField] private string zoneName = "";
    [SerializeField] private GameObject interactPrompt; // "Appuie sur E pour sauvegarder"

    private bool _playerInRange = false;
    private bool _saving = false;

    private void Start()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        if (interactPrompt != null)
            interactPrompt.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!_playerInRange || _saving) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            DialogueUI.Instance.Show(
                "Voulez-vous sauvegarder votre partie ?",
                () => SaveAsync()
            );
        }
    }

    private async void SaveAsync()
    {
        _saving = true;

        var pos = FindObjectOfType<PlayerMovement>()?.transform.position ?? Vector3.zero;
        GameManager.Instance.PosX = pos.x;
        GameManager.Instance.PosY = pos.y;
        GameManager.Instance.CurrentZone = string.IsNullOrEmpty(zoneName)
            ? SceneManager.GetActiveScene().name : zoneName;

        await PlayerService.Instance.SyncAsync();
        _saving = false;
    }
}