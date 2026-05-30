using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action _onConfirm;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        dialoguePanel.SetActive(false);
    }

    public void Show(string message, Action onConfirm)
    {
        _onConfirm = onConfirm;
        messageText.text = message;
        dialoguePanel.SetActive(true);
    }

    public void OnYesClicked()
    {
        dialoguePanel.SetActive(false);
        _onConfirm?.Invoke();
    }

    public void OnNoClicked()
    {
        dialoguePanel.SetActive(false);
    }
}