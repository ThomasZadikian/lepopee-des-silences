using RPG.Core;
using UnityEngine;
using UnityEngine.InputSystem;

public class HealingItem : MonoBehaviour
{
    [SerializeField] private int healAmount = 30;
    [SerializeField] private GameObject interactPrompt;

    private bool _playerInRange = false;

    private void Start()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Heal] Trigger touché par : {other.gameObject.name} tag: {other.gameObject.tag}");

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
        if (!_playerInRange) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            var p = GameManager.Instance.Player;
            DialogueUI.Instance.Show(
                $"Utiliser une potion ? (+{healAmount} HP)",
                () =>
                {
                    p.CurrentHP = Mathf.Min(p.MaxHP, p.CurrentHP + healAmount);
                    Debug.Log($"[Heal] +{healAmount} HP. HP actuels : {p.CurrentHP}/{p.MaxHP}");
                }
            );
        }
    }
}