using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Core;
using UnityEngine.InputSystem;

public class HUD : MonoBehaviour
{
    [SerializeField] private Slider hpBar;
    [SerializeField] private Slider mpBar;
    [SerializeField] private Slider xpBar;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text mpText;

    [SerializeField] private GameObject hudPanel; // Le panel parent qui contient tout le HUD

    private bool _hudVisible = true;

    private void Update()
    {
        // Toggle HUD avec la touche X
        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            _hudVisible = !_hudVisible;
            if (hudPanel != null)
                hudPanel.SetActive(_hudVisible);
        }

        if (!_hudVisible) return;
        if (GameManager.Instance == null) return;

        var p = GameManager.Instance.Player;

        if (hpBar != null) hpBar.value = p.MaxHP > 0 ? (float)p.CurrentHP / p.MaxHP : 0;
        if (hpText != null) hpText.text = $"{p.CurrentHP} / {p.MaxHP}";
        if (mpBar != null) mpBar.value = p.MaxMP > 0 ? (float)p.CurrentMP / p.MaxMP : 0;
        if (mpText != null) mpText.text = $"{p.CurrentMP} / {p.MaxMP}";

        int xpRequired = p.Level * 100;
        if (xpBar != null) xpBar.value = xpRequired > 0 ? (float)p.Experience / xpRequired : 0;
        if (goldText != null) goldText.text = $"{p.Gold} G";
        if (levelText != null) levelText.text = $"Lv. {p.Level}";
    }
}