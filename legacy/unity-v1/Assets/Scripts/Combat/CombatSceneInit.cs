using RPG.Core;
using System.Collections.Generic;
using UnityEngine;

public class CombatSceneInit : MonoBehaviour
{
    private void Start()
    {
        var player = GameManager.Instance.CurrentEnemy;
        if (player == null)
        {
            Debug.LogError("[Combat] Aucun ennemi defini.");
            return;
        }

        var combatants = new List<Combatant>
        {
            Combatant.FromPlayer(GameManager.Instance.Player),
            Combatant.FromEnemy(GameManager.Instance.CurrentEnemy)
        };

        // Initialiser le boss si c'est un combat de boss
        var bossController = FindFirstObjectByType<BossController>();
        if (bossController != null)
        {
            var bossCombatant = combatants.Find(c => !c.isPlayer);
            bossController.Initialize(bossCombatant);
        }

        var combatSystem = gameObject.AddComponent<CombatSystem>();
        ATBManager.Instance.Initialize(combatants, combatSystem);
        CombatUIManager.Instance.Initialize(combatants);

        Debug.Log("[Combat] Scene initialisee.");
    }
}