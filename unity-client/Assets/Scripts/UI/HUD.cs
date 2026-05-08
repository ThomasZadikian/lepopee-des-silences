using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Core;

public class HUD : MonoBehaviour
{
    [SerializeField] private Slider hpBar;
    [SerializeField] private Slider mpBar;
    [SerializeField] private Slider xpBar;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text mpText;



    private void Update()
    {
        if (GameManager.Instance == null) return;

        var p = GameManager.Instance.Player;

        if (hpBar != null) hpBar.value = (float)p.CurrentHP / p.MaxHP;
        if (hpText != null) hpText.text = $"{p.CurrentHP} / {p.MaxHP}";
        if (mpBar != null) mpBar.value = (float)p.CurrentMP / p.MaxMP;
        if (mpText != null) mpText.text = $"{p.CurrentMP} / {p.MaxMP}";


        int xpRequired = p.Level * 100;
        if (xpBar != null) xpBar.value = xpRequired > 0 ? (float)p.Experience / xpRequired : 0;

        if (goldText != null) goldText.text = $"{p.Gold} G";
        if (levelText != null) levelText.text = $"Lv. {p.Level}";
    }
}